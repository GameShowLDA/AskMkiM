using Ask.Engine.ControlCommandAnalyser;
using Ask.UI.Controls.TextEditorControl.Diagnostics;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Ask.UI.Controls.TextEditorControl.Suggestions;

/// <summary>
/// Отображает предлагаемое диагностикой изменение и принимает его по клавише <see cref="Key.Tab"/>.
/// </summary>
internal sealed class InlineCommandSuggestionController : IBackgroundRenderer
{
  private readonly TextEditor _editor;
  private SourceTextSuggestion? _currentSuggestion;

  /// <summary>
  /// Инициализирует контроллер автодополнения для указанного редактора.
  /// </summary>
  /// <param name="editor">Редактор исходной программы контроля.</param>
  public InlineCommandSuggestionController(TextEditor editor)
  {
    _editor = editor;

    editor.TextArea.TextView.BackgroundRenderers.Add(this);
    editor.TextChanged += OnDismissSuggestion;
    editor.DocumentChanged += OnDismissSuggestion;
    editor.TextArea.Caret.PositionChanged += OnDismissSuggestion;
    editor.PreviewKeyDown += OnPreviewKeyDown;
  }

  /// <summary>
  /// Возвращает слой, на котором отображается полупрозрачный текст автодополнения.
  /// </summary>
  public KnownLayer Layer => KnownLayer.Selection;

  /// <summary>
  /// Подключает контроллер к событиям наведения на диагностические предупреждения.
  /// </summary>
  /// <param name="diagnostics">Контроллер фоновой диагностики редактора.</param>
  public void Attach(LiveDiagnosticsController diagnostics) =>
    diagnostics.HoverDiagnosticsChanged += OnHoverDiagnosticsChanged;

  /// <summary>
  /// Скрывает активное автодополнение.
  /// </summary>
  public void Dismiss() => SetSuggestion(null);

  /// <summary>
  /// Применяет активное исправление к документу.
  /// </summary>
  /// <returns><see langword="true"/>, если исправление было применено.</returns>
  internal bool TryAcceptCurrentSuggestion()
  {
    if (_currentSuggestion is not { } suggestion
      || _editor.IsReadOnly
      || suggestion.Offset < 0
      || suggestion.ReplacementLength < 0
      || suggestion.Offset + suggestion.ReplacementLength > _editor.Document.TextLength)
    {
      return false;
    }

    _editor.Document.Replace(suggestion.Offset, suggestion.ReplacementLength, suggestion.Text);
    _editor.CaretOffset = suggestion.Offset + suggestion.Text.Length;
    return true;
  }

  /// <summary>
  /// Рисует предложенное исправление, не изменяя документ.
  /// </summary>
  /// <param name="textView">Визуальное представление текста.</param>
  /// <param name="drawingContext">Контекст рисования слоя редактора.</param>
  public void Draw(TextView textView, DrawingContext drawingContext)
  {
    if (_currentSuggestion is not { } suggestion || !textView.VisualLinesValid)
    {
      return;
    }

    int previewOffset = suggestion.PreviewOffset ?? suggestion.Offset;
    if (previewOffset < 0 || previewOffset > _editor.Document.TextLength)
    {
      return;
    }

    var documentLine = _editor.Document.GetLineByOffset(previewOffset);
    if (textView.GetVisualLine(documentLine.LineNumber) == null)
    {
      return;
    }

    var location = _editor.Document.GetLocation(previewOffset);
    var documentPosition = textView.GetVisualPosition(
      new TextViewPosition(location),
      VisualYPosition.TextTop);
    var viewportPosition = documentPosition - textView.ScrollOffset;
    if (suggestion.PreviewOnNextLine)
    {
      viewportPosition.Y += textView.DefaultLineHeight;
      viewportPosition.X = 0;
    }

    var typeface = new Typeface(
      _editor.FontFamily,
      _editor.FontStyle,
      _editor.FontWeight,
      _editor.FontStretch);
    var formattedText = new FormattedText(
      suggestion.PreviewText,
      CultureInfo.CurrentUICulture,
      _editor.FlowDirection,
      typeface,
      _editor.FontSize,
      CreateSuggestionBrush(),
      VisualTreeHelper.GetDpi(_editor).PixelsPerDip);

    drawingContext.DrawText(formattedText, viewportPosition);
  }

  private void OnHoverDiagnosticsChanged(IReadOnlyList<SourceDiagnostic> diagnostics) =>
    UpdateFromDiagnostics(diagnostics);

  internal void UpdateFromDiagnostics(IReadOnlyList<SourceDiagnostic> diagnostics)
  {
    var suggestion = diagnostics
      .Select(diagnostic => diagnostic.Suggestion)
      .FirstOrDefault(candidate => candidate != null);
    SetSuggestion(suggestion);
  }

  private void OnDismissSuggestion(object? sender, EventArgs e) => Dismiss();

  private void OnPreviewKeyDown(object sender, KeyEventArgs e)
  {
    if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None && TryAcceptCurrentSuggestion())
    {
      e.Handled = true;
      return;
    }

    if (e.Key == Key.Escape && _currentSuggestion != null)
    {
      Dismiss();
      e.Handled = true;
    }
  }

  private void SetSuggestion(SourceTextSuggestion? suggestion)
  {
    if (_currentSuggestion == suggestion)
    {
      return;
    }

    _currentSuggestion = suggestion;
    _editor.TextArea.TextView.InvalidateLayer(Layer);
  }

  private Brush CreateSuggestionBrush()
  {
    var brush = (_editor.Foreground ?? Brushes.Gray).CloneCurrentValue();
    brush.Opacity *= 0.45;
    brush.Freeze();
    return brush;
  }
}
