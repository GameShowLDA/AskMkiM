using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Engine.ControlCommandAnalyser;
using ICSharpCode.AvalonEdit;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
  private readonly ToolTip _toolTip = new() { Placement = PlacementMode.Mouse, MaxWidth = 620 };
  private CancellationTokenSource? _pending;
  private bool _enabled;

  public LiveDiagnosticsController(TextEditor editor)
    : this(editor, (text, token) => new CommandTranslationManager().AnalyzeSource(text, token)) { }

  internal LiveDiagnosticsController(TextEditor editor,
    Func<string, CancellationToken, IReadOnlyList<SourceDiagnostic>> analyze)
  {
    _editor = editor;
    _analyze = analyze;
    _renderer = new DiagnosticUnderlineRenderer(editor.TextArea.TextView);
    editor.TextArea.TextView.BackgroundRenderers.Add(_renderer);
    editor.TextChanged += OnTextChanged;
    editor.DocumentChanged += OnTextChanged;
    editor.Loaded += OnLoaded;
    editor.Unloaded += OnUnloaded;
    editor.MouseHover += OnMouseHover;
    editor.MouseHoverStopped += OnMouseHoverStopped;
    editor.MouseLeave += OnMouseHoverStopped;
  }

  public void Configure(FileType fileType)
  {
    _enabled = fileType is FileType.None or FileType.PK or FileType.PKW;
    Refresh();
  }

  public void Refresh()
  {
    CancelPending();
    if (!_enabled || _editor.IsReadOnly || !_editor.IsLoaded || _editor.Document == null) return;
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
      // Access the WPF document only on its dispatcher. Parsing sees plain text.
      var document = _editor.Document;
      string snapshot = document.Text;
      var diagnostics = await Task.Run(async () =>
      {
        await AnalysisGate.WaitAsync(token).ConfigureAwait(false);
        try
        {
          token.ThrowIfCancellationRequested();
          return _analyze(snapshot, token);
        }
        finally { AnalysisGate.Release(); }
      }, token);

      if (!token.IsCancellationRequested && ReferenceEquals(_pending, cancellation)
        && ReferenceEquals(_editor.Document, document) && _editor.IsLoaded && !_editor.IsReadOnly)
        _renderer.SetDiagnostics(diagnostics);
    }
    catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    catch (Exception ex)
    {
      Ask.LogLib.LoggerUtility.LogWarning($"Ошибка фоновой проверки текста: {ex.Message}");
      if (!token.IsCancellationRequested && ReferenceEquals(_pending, cancellation)
        && _editor.IsLoaded && !_editor.IsReadOnly
        && _editor.Document?.Lines.FirstOrDefault(documentLine => documentLine.Length > 0) is { } line)
      {
        _renderer.SetDiagnostics(new[]
        {
          new SourceDiagnostic(line.Offset, line.Length, line.LineNumber, true,
            "Не удалось проверить текст. Выполните трансляцию для подробной диагностики.", null),
        });
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
    _renderer.SetDiagnostics(Array.Empty<SourceDiagnostic>());
    _toolTip.IsOpen = false;
  }

  private void OnLoaded(object sender, RoutedEventArgs e) => Refresh();
  private void OnUnloaded(object sender, RoutedEventArgs e) => CancelPending();
  private void OnTextChanged(object? sender, EventArgs e) => Refresh();
  private void OnMouseHoverStopped(object sender, MouseEventArgs e) => _toolTip.IsOpen = false;

  private void OnMouseHover(object sender, MouseEventArgs e)
  {
    var position = _editor.GetPositionFromPoint(e.GetPosition(_editor));
    if (position == null || _editor.Document == null) return;
    int offset = _editor.Document.GetOffset(position.Value.Location);
    var diagnostics = _renderer.GetDiagnosticsAt(offset);
    if (diagnostics.Count == 0) return;
    _toolTip.Content = new TextBlock
    {
      Text = string.Join(Environment.NewLine, diagnostics.Select(d =>
        $"{(d.IsWarning ? "Предупреждение" : "Ошибка")}: {d.Description}").Distinct()),
      TextWrapping = TextWrapping.Wrap,
      MaxWidth = 600,
    };
    _toolTip.PlacementTarget = _editor;
    _toolTip.IsOpen = true;
    e.Handled = true;
  }
}
