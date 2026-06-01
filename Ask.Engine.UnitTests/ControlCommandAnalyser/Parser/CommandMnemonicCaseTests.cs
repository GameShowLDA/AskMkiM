using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser;

public class CommandMnemonicCaseTests : IDisposable
{
  [Fact(DisplayName = "Парсер: строчные мнемоники команд распознаются и нормализуются")]
  public void ParseAll_WithLowercaseCyrillicMnemonics_NormalizesToUppercase()
  {
    var manager = new CommandTranslationManager();
    var models = manager.ParseAll(string.Join("\n", new[]
    {
      "10 \u043e\u043a TEST.000.000 * TEST",
      "20 \u0441\u043f TOTAL",
      "30 \u0440\u043c X1/1=1.1.1",
      "80 \u0446\u0443 Check message",
      "90 \u0441\u0438 100\u0412, 1<\u041c\u041e\u043c *X1/1*",
      "100 \u043e\u0441",
      "110 \u043a\u0446 Finish",
    }));

    Assert.DoesNotContain(models, model => model is UnknownCommandModel);
    Assert.Contains(models, model => model.CommandNumber == "80" && model.Mnemonic == "\u0426\u0423");
    Assert.Contains(models, model => model.CommandNumber == "90" && model.Mnemonic == "\u0421\u0418");

    var cuCommand = models.Single(model => model.CommandNumber == "80");
    Assert.StartsWith("80 \u0426\u0423", cuCommand.SourceLines[0]);
  }

  public void Dispose()
  {
    CommandsModel.Clear();
  }
}
