using Ask.Core.Services.Config.Base;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Engine.ControlCommandAnalyser;
using ICSharpCode.AvalonEdit;
using System.Windows;
using System.Windows.Input;

namespace Ask.UI.Controls.TextEditorControl.Diagnostics;

/// <summary>Debounced snapshot analysis shared by both editor hosts.</summary>
public sealed class LiveDiagnosticsController
{
  // Bound the amount of background parser work when several tabs are open.
  private static readonly SemaphoreSlim AnalysisGate = new(1, 1);
  private readonly TextEditor _editor;
  private readonly DiagnosticUnderlineRenderer _renderer;
  private readonly Func<string, CancellationToken, IReadOnlyList<SourceDiagnostic>> _analyze;
  private readonly DiagnosticHoverPopup _hoverPopup;
  private Window? _hostWindow;
  private CancellationTokenSource? _pending;
  private bool _enabled;
  private bool _settingsSubscribed;
  private DiagnosticSnapshot _diagnostics = DiagnosticSnapshot.Empty;

  public LiveDiagnosticsController(TextEditor editor)
    : this(editor, (text, token) => new CommandTranslationManager().AnalyzeSource(text, token)) { }

  internal LiveDiagnosticsController(TextEditor editor,
    Func<string, CancellationToken, IReadOnlyList<SourceDiagnostic>> analyze)
  {
    _editor = editor;
    _analyze = analyze;
    _renderer = new DiagnosticUnderlineRenderer(editor.TextArea.TextView);
    _hoverPopup = new DiagnosticHoverPopup(editor.TextArea.TextView);
    editor.TextArea.TextView.BackgroundRenderers.Add(_renderer);
    editor.TextChanged += OnTextChanged;
    editor.DocumentChanged += OnTextChanged;
    editor.Loaded += OnLoaded;
    editor.Unloaded += OnUnloaded;
    editor.MouseHover += OnMouseHover;
    editor.MouseMove += OnMouseMove;
    editor.MouseLeave += (_, _) => _hoverPopup.ScheduleClose();
    editor.PreviewMouseDown += OnDismissHover;
    editor.PreviewKeyDown += OnPreviewKeyDown;
    editor.PreviewMouseWheel += OnDismissHover;
    editor.SizeChanged += OnDismissHover;
    editor.TextArea.TextView.ScrollOffsetChanged += OnDismissHover;
    // Opening a Popup can itself rebuild visual lines during WPF layout.
    // Close for user navigation/resize, not for every VisualLinesChanged event.
    if (editor.IsLoaded)
    {
      AttachWindow();
      SubscribeToSettings();
    }
  }

  public void Configure(FileType fileType, bool enabled = true)
  {
    _enabled = enabled && (fileType is FileType.None or FileType.PK or FileType.PKW);
    Refresh();
  }

  public void Refresh()
  {
    CancelPending();
    if (!_enabled || _editor.IsReadOnly || !_editor.IsLoaded || _editor.Document == null) return;
    if (!UserInterfaceConfig.GetSyntaxErrorUnderlining() && !UserInterfaceConfig.GetStyleErrorUnderlining()) return;
    var cancellation = new CancellationTokenSource();
    _pending = cancellation;
    _ = AnalyzeAsync(cancellation);
  }

  private async Task AnalyzeAsync(CancellationTokenSource cancellation)
  {
    var token = cancellation.Token;
    try
    {
      await Task.Delay(400, token);
      // Capture AvalonEdit's immutable rope cheaply; materialize the string on the worker.
      var document = _editor.Document;
      var snapshot = document.CreateSnapshot();
      var diagnostics = await Task.Run(async () =>
      {
        await AnalysisGate.WaitAsync(token).ConfigureAwait(false);
        try
        {
          token.ThrowIfCancellationRequested();
          return DiagnosticSnapshot.Create(_analyze(snapshot.Text, token), token);
        }
        finally { AnalysisGate.Release(); }
      }, token);

      if (!token.IsCancellationRequested && ReferenceEquals(_pending, cancellation)
        && ReferenceEquals(_editor.Document, document) && _editor.IsLoaded && !_editor.IsReadOnly)
        SetDiagnostics(diagnostics);
    }
    catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    catch (Exception ex)
    {
      Ask.LogLib.LoggerUtility.LogWarning($"Ошибка фоновой проверки текста: {ex.Message}");
      if (!token.IsCancellationRequested && ReferenceEquals(_pending, cancellation)
        && _editor.IsLoaded && !_editor.IsReadOnly
        && _editor.Document?.Lines.FirstOrDefault(documentLine => documentLine.Length > 0) is { } line)
      {
        SetDiagnostics(DiagnosticSnapshot.Create(new[]
        {
          new SourceDiagnostic(line.Offset, line.Length, line.LineNumber, true,
            "Не удалось проверить текст. Выполните трансляцию для подробной диагностики.", null),
        }));
      }
    }
    finally
    {
      if (ReferenceEquals(_pending, cancellation)) _pending = null;
      cancellation.Dispose();
    }
  }

  private void CancelPending()
  {
    _pending?.Cancel();
    _pending = null;
    SetDiagnostics(DiagnosticSnapshot.Empty);
    _hoverPopup.Close();
  }

  private void SetDiagnostics(DiagnosticSnapshot diagnostics)
  {
    _diagnostics = diagnostics;
    ApplyDiagnosticVisibility();
  }

  private void ApplyDiagnosticVisibility()
  {
    _hoverPopup.Close();
    bool showErrors = UserInterfaceConfig.GetSyntaxErrorUnderlining();
    bool showWarnings = UserInterfaceConfig.GetStyleErrorUnderlining();
    _renderer.SetDiagnostics(_diagnostics, showErrors, showWarnings);
  }

  private void SubscribeToSettings()
  {
    if (_settingsSubscribed) return;
    EventAggregator.Subscribe<EditorEvents.DiagnosticUnderliningChanged>(OnDiagnosticUnderliningChanged);
    _settingsSubscribed = true;
  }

  private void OnDiagnosticUnderliningChanged(EditorEvents.DiagnosticUnderliningChanged _)
  {
    if (_editor.Dispatcher.CheckAccess()) RefreshVisibility();
    else _editor.Dispatcher.InvokeAsync(() =>
    {
      if (_editor.IsLoaded) RefreshVisibility();
    });
  }

  private void RefreshVisibility()
  {
    if (!UserInterfaceConfig.GetSyntaxErrorUnderlining() && !UserInterfaceConfig.GetStyleErrorUnderlining())
      CancelPending();
    else if (ReferenceEquals(_diagnostics, DiagnosticSnapshot.Empty) && _pending == null)
      Refresh();
    else
      ApplyDiagnosticVisibility();
  }

  private void OnLoaded(object sender, RoutedEventArgs e)
  {
    AttachWindow();
    SubscribeToSettings();
    Refresh();
  }

  private void OnUnloaded(object sender, RoutedEventArgs e)
  {
    CancelPending();
    DetachWindow();
    EventAggregator.Unsubscribe<EditorEvents.DiagnosticUnderliningChanged>(OnDiagnosticUnderliningChanged);
    _settingsSubscribed = false;
  }

  private void OnTextChanged(object? sender, EventArgs e) => Refresh();

  private void OnPreviewKeyDown(object sender, KeyEventArgs e)
  {
    _hoverPopup.Close();
    if (e.Handled || !_enabled || _editor.Document == null) return;

    var key = e.Key == Key.System ? e.SystemKey : e.Key;
    var modifiers = Keyboard.Modifiers;
    if (key != Key.F8 || (modifiers != ModifierKeys.None && modifiers != ModifierKeys.Shift)) return;

    bool showErrors = UserInterfaceConfig.GetSyntaxErrorUnderlining();
    bool showWarnings = UserInterfaceConfig.GetStyleErrorUnderlining();
    var target = DiagnosticNavigator.FindTarget(
      _diagnostics.GetNavigationDiagnostics(showErrors, showWarnings),
      _editor.CaretOffset,
      _editor.SelectionStart,
      _editor.SelectionLength,
      previous: modifiers == ModifierKeys.Shift);

    var document = _editor.Document;
    if (target == null || target.Offset < 0 || target.Offset >= document.TextLength) return;

    int length = Math.Min(target.Length, document.TextLength - target.Offset);
    if (length <= 0) return;

    int lineNumber = document.GetLineByOffset(target.Offset).LineNumber;
    _editor.ScrollToLine(lineNumber);
    _editor.Select(target.Offset, length);
    _editor.Focus();
    e.Handled = true;
  }

  private void OnDismissHover(object? sender, EventArgs e)
  {
    // Popup input can route through its placement target. Allow scrolling the card.
    if (e is MouseEventArgs && _hoverPopup.Content.IsMouseOver) return;
    _hoverPopup.Close();
  }

  private void AttachWindow()
  {
    DetachWindow();
    _hostWindow = Window.GetWindow(_editor);
    if (_hostWindow == null) return;
    _hostWindow.Deactivated += OnDismissHover;
    _hostWindow.LocationChanged += OnDismissHover;
  }

  private void DetachWindow()
  {
    if (_hostWindow == null) return;
    _hostWindow.Deactivated -= OnDismissHover;
    _hostWindow.LocationChanged -= OnDismissHover;
    _hostWindow = null;
  }

  private void OnMouseHover(object sender, MouseEventArgs e)
  {
    if (UpdateHover(e.GetPosition(_editor.TextArea.TextView))) e.Handled = true;
  }

  private void OnMouseMove(object sender, MouseEventArgs e)
  {
    if (_hoverPopup.IsOpen && !_hoverPopup.Content.IsMouseOver)
      UpdateHover(e.GetPosition(_editor.TextArea.TextView));
  }

  internal DiagnosticHoverPopup HoverPopup => _hoverPopup;

  internal bool UpdateHover(Point point)
  {
    var diagnostics = _renderer.GetDiagnosticsAt(point, out var anchor);
    if (diagnostics.Count == 0)
    {
      _hoverPopup.ScheduleClose();
      return false;
    }
    _hoverPopup.Show(diagnostics, anchor);
    return true;
  }
}
