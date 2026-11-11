using UI.Controls.TextEditorControl;
using UI.Services;

namespace Ask.UI.UnitTests.Services.FileManager;

public sealed class TranslationInputNormalizerTests
{
  [Fact]
  public void TechnicalTranslationNormalizationDoesNotChangeEditorBuffer() => RunInSta(() =>
  {
    const string source = "10 ОК  ТЕСТ\r\n\t     Описание\r\n20 КЦ";
    var editor = new TextEditorUI { Text = source };

    string translationInput = TranslationInputNormalizer.Normalize(editor.Text);

    Assert.NotEqual(source, translationInput);
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
      throw new InvalidOperationException("Проверка подготовки текста трансляции завершилась ошибкой.", exception);
    }
  }
}
