using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Cu;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser.Cu;

public sealed class CuCommandParserTests
{
  [Theory]
  [InlineData("Тест?", "Тест")]
  [InlineData("Тест??", "Тест?")]
  [InlineData("Тест???", "Тест??")]
  [InlineData("Тест??   ", "Тест?")]
  [InlineData("Тест ?", "Тест")]
  public void Parse_Question_RemovesOnlyLastQuestionMark(string sourceText, string expectedText)
  {
    var parser = new CuCommandParser();
    var source = $"40 ЦУ {sourceText}";

    var model = Assert.IsType<CuCommandModel>(parser.Parse("40", "ЦУ", 1, [source]));

    Assert.Equal(CuCommandType.Question, model.CuType);
    Assert.Equal(expectedText, model.MessageText);
    Assert.Equal(source, Assert.Single(model.SourceLines));
  }

  [Fact]
  public void Parse_Information_PreservesMessageText()
  {
    var parser = new CuCommandParser();

    var model = Assert.IsType<CuCommandModel>(parser.Parse("40", "ЦУ", 1, ["40 ЦУ Тест"]));

    Assert.Equal(CuCommandType.Information, model.CuType);
    Assert.Equal("Тест", model.MessageText);
  }
}
