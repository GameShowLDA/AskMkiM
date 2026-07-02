using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Translator;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Ck;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser.Ck
{
  public class CkCommandParserTests
  {
    [Fact]
    public void CanParse_CkMnemonic_ReturnsTrue()
    {
      var parser = new CkCommandParser();

      Assert.True(parser.CanParse(new MnemonicIdentifier("СК")));
      Assert.False(parser.CanParse(new MnemonicIdentifier("ОТ")));
    }

    [Fact]
    public void Parse_Key_ParsesCorrectly()
    {
      var parser = new CkCommandParser();

      var model = Assert.IsType<CkCommandModel>(
          parser.Parse(
              "10",
              "СК",
              1,
              new List<string>
              {
                        "10 СК Д"
              }));

      Assert.Contains(model.Errors,
          e => e.Code == ErrorCode.Gen_WrongKey);
    }

    [Fact]
    public void Parse_DuplicateKey_AddsWarning()
    {
      var parser = new CkCommandParser();

      var model = Assert.IsType<CkCommandModel>(
          parser.Parse(
              "10",
              "СК",
              1,
              new List<string>
              {
                        "10 СК Д,Д"
              }));

      Assert.NotEmpty(model.Warnings);
    }

    [Fact]
    public void Parse_InvalidKey_AddsError()
    {
      var parser = new CkCommandParser();

      var model = Assert.IsType<CkCommandModel>(
          parser.Parse(
              "10",
              "СК",
              1,
              new List<string>
              {
                        "10 СК Б"
              }));

      Assert.NotEmpty(model.Errors);
    }

    [Fact]
    public void Parse_UnparsedParameters_AddsError()
    {
      var parser = new CkCommandParser();

      var model = Assert.IsType<CkCommandModel>(
          parser.Parse(
              "10",
              "СК",
              1,
              new List<string>
              {
                        "10 СК qwerty"
              }));

      Assert.Contains(model.Errors,
          e => e.Code == ErrorCode.Gen_UnrecognizedParameters);
    }
  }
}
