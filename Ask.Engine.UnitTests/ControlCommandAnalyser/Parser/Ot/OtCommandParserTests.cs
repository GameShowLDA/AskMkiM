using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Translator;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Ot;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser.Ot
{
  public class OtCommandParserTests
  {
    [Fact]
    public void CanParse_OtMnemonic_ReturnsTrue()
    {
      var parser = new OtCommandParser();

      Assert.True(parser.CanParse(new MnemonicIdentifier("ОТ")));
      Assert.False(parser.CanParse(new MnemonicIdentifier("ПТ")));
    }

    [Fact]
    public void Parse_Time_ParsesCorrectly()
    {
      var parser = new OtCommandParser();

      var model = Assert.IsType<OtCommandModel>(
          parser.Parse(
              "10",
              "ОТ",
              1,
              new List<string>
              {
                        "10 ОТ 5с"
              }));

      Assert.Equal(5, model.Time);
      Assert.Equal("5с", model.TimeSource);
    }

    [Fact]
    public void Parse_Key_ParsesCorrectly()
    {
      var parser = new OtCommandParser();

      var model = Assert.IsType<OtCommandModel>(
          parser.Parse(
              "10",
              "ОТ",
              1,
              new List<string>
              {
                        "10 ОТ Б"
              }));

      Assert.Contains("Б", model.AlgorithmKey);
    }

    [Fact]
    public void Parse_DuplicateKey_AddsWarning()
    {
      var parser = new OtCommandParser();

      var model = Assert.IsType<OtCommandModel>(
          parser.Parse(
              "10",
              "ОТ",
              1,
              new List<string>
              {
                        "10 ОТ Б,Б"
              }));

      Assert.NotEmpty(model.Warnings);
    }

    [Fact]
    public void Parse_InvalidKey_AddsError()
    {
      var parser = new OtCommandParser();

      var model = Assert.IsType<OtCommandModel>(
          parser.Parse(
              "10",
              "ОТ",
              1,
              new List<string>
              {
                        "10 ОТ К"
              }));

      Assert.NotEmpty(model.Errors);
    }

    [Fact]
    public void Parse_UnparsedParameters_AddsError()
    {
      var parser = new OtCommandParser();

      var model = Assert.IsType<OtCommandModel>(
          parser.Parse(
              "10",
              "ОТ",
              1,
              new List<string>
              {
                        "10 ОТ qwerty"
              }));

      Assert.Contains(model.Errors,
        e => e.Code == ErrorCode.Gen_UnrecognizedParameters);
    }
  }
}
