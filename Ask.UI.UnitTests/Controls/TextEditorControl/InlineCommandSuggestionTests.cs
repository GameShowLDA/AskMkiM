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
