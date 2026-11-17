using Ask.Engine.ControlCommandAnalyser;
using Ask.UI.Controls.TextEditorControl.Suggestions;
using ICSharpCode.AvalonEdit;

namespace Ask.UI.UnitTests.Controls.TextEditorControl;

public sealed class InlineCommandSuggestionTests
{
  [Fact]
  public void HoverSuggestionInsertsFullCommandAsSingleUndoableEdit() => RunInSta(() =>
  {
    const string source = "30 РМ K1/1-100=1.6.1-100\r\n\r\n40 ЦУ Текст";
    int insertionOffset = source.IndexOf("\r\n\r\n", StringComparison.Ordinal) + 2;
    var editor = new TextEditor { Text = source };
    editor.Document.UndoStack.ClearAll();
    var controller = new InlineCommandSuggestionController(editor);
    controller.UpdateFromDiagnostics(new[]
    {
      new SourceDiagnostic(
        3,
        2,
        1,
        true,
        "Команда ВШ была добавлена автоматически.",
        "Gen_VshCommandAddedAutomatically",
        new SourceTextSuggestion(insertionOffset, "31 ВШ 2Ш\r\n", "31 ВШ 2Ш")),
    });

    Assert.True(controller.TryAcceptCurrentSuggestion());
    Assert.Equal(
      "30 РМ K1/1-100=1.6.1-100\r\n31 ВШ 2Ш\r\n\r\n40 ЦУ Текст",
      editor.Text);

    editor.Document.UndoStack.Undo();
    Assert.Equal(source, editor.Text);
  });

  [Fact]
  public void DiagnosticWithoutSourceSuggestionDoesNotChangeDocument() => RunInSta(() =>
  {
    var editor = new TextEditor { Text = "30 РМ" };
    var controller = new InlineCommandSuggestionController(editor);
    controller.UpdateFromDiagnostics(new[]
    {
      new SourceDiagnostic(3, 2, 1, true, "Другое предупреждение.", "OtherWarning"),
    });

    Assert.False(controller.TryAcceptCurrentSuggestion());
    Assert.Equal("30 РМ", editor.Text);
  });

  [Fact]
  public void HoverSuggestionInsertsPrAlgorithmKey() => RunInSta(() =>
  {
    const string source = "180 ПР Ом<5 *К1/11*";
    int insertionOffset = source.IndexOf("ПР", StringComparison.Ordinal) + "ПР".Length;
    var editor = new TextEditor { Text = source };
    var controller = new InlineCommandSuggestionController(editor);
    controller.UpdateFromDiagnostics(new[]
    {
      new SourceDiagnostic(
        4,
        2,
        1,
        true,
        "Добавлен ключ ЗР.",
        "Gen_KeyZR",
        new SourceTextSuggestion(insertionOffset, " ЗР", " ЗР")),
    });

    Assert.True(controller.TryAcceptCurrentSuggestion());
    Assert.Equal("180 ПР ЗР Ом<5 *К1/11*", editor.Text);
  });

  [Fact]
  public void HoverSuggestionReplacesCorrectedSourceLineAsSingleUndoableEdit() => RunInSta(() =>
  {
    const string source = "180 ПР 10В, Ом<5 *К1/11*";
    const string corrected = "180 ПР Ом<5 *К1/11*";
    var editor = new TextEditor { Text = source };
    editor.Document.UndoStack.ClearAll();
    var controller = new InlineCommandSuggestionController(editor);
    controller.UpdateFromDiagnostics(new[]
    {
      new SourceDiagnostic(
        7,
        3,
        1,
        true,
        "Значение напряжения игнорируется.",
        "Gen_IgnoreVoltage",
        new SourceTextSuggestion(
          0,
          corrected,
          $"  → {corrected}",
          ReplacementLength: source.Length,
          PreviewOffset: source.Length)),
    });

    Assert.True(controller.TryAcceptCurrentSuggestion());
    Assert.Equal(corrected, editor.Text);

    editor.Document.UndoStack.Undo();
    Assert.Equal(source, editor.Text);
  });

  private static void RunInSta(Action action)
  {
    Exception? exception = null;
    var thread = new Thread(() =>
    {
      try
      {
        action();
      }
      catch (Exception error)
      {
        exception = error;
      }
    }) { IsBackground = true };
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
    if (exception != null)
    {
      throw new InvalidOperationException("Проверка автодополнения завершилась ошибкой.", exception);
    }
  }
}
