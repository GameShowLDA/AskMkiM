using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Translator;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Pt;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser.Pt
{
  public class PtCommandParserTests
  {
    [Fact]
    public void CanParse_PtMnemonic_ReturnsTrue()
    {
      var parser = new PtCommandParser();

      Assert.True(parser.CanParse(new MnemonicIdentifier("ПТ")));
      Assert.False(parser.CanParse(new MnemonicIdentifier("ОТ")));
    }

    [Fact]
    public void Parse_Time_ParsesCorrectly()
    {
      var parser = new PtCommandParser();

      var model = Assert.IsType<PtCommandModel>(
          parser.Parse(
              "10",
              "ПТ",
              1,
              new List<string>
              {
                        "10 ПТ 5с"
              }));

      Assert.Equal(5, model.Time);
      Assert.Equal("5с", model.TimeSource);
    }

    [Fact]
    public void Parse_Key_ParsesCorrectly()
    {
      var parser = new PtCommandParser();

      var model = Assert.IsType<PtCommandModel>(
          parser.Parse(
              "10",
              "ПТ",
              1,
              new List<string>
              {
                        "10 ПТ Б"
              }));

      Assert.Contains("Б", model.AlgorithmKey);
    }

    [Fact]
    public void Parse_DuplicateKey_AddsWarning()
    {
      var parser = new PtCommandParser();

      var model = Assert.IsType<PtCommandModel>(
          parser.Parse(
              "10",
              "ПТ",
              1,
              new List<string>
              {
                        "10 ПТ Б,Б"
              }));

      Assert.NotEmpty(model.Warnings);
    }

    [Fact]
    public void Parse_InvalidKey_AddsError()
    {
      var parser = new PtCommandParser();

      var model = Assert.IsType<PtCommandModel>(
          parser.Parse(
              "10",
              "ПТ",
              1,
              new List<string>
              {
                        "10 ПТ К"
              }));

      Assert.NotEmpty(model.Errors);
    }

    [Fact]
    public void Parse_UnparsedParameters_Saved()
    {
      var parser = new PtCommandParser();

      var model = Assert.IsType<PtCommandModel>(
          parser.Parse(
              "10",
              "ПТ",
              1,
              new List<string>
              {
                        "10 ПТ qwerty"
              }));

      Assert.False(string.IsNullOrWhiteSpace(model.UnparsedParameters));
    }

    [Fact]
    public void Parse_DuplicateTime_AddsWarning()
    {
      var parser = new PtCommandParser();

      var model = Assert.IsType<PtCommandModel>(
          parser.Parse(
              "10",
              "ПТ",
              1,
              new List<string>
              {
                "10 ПТ 1с 2с"
              }));

      Assert.Contains(
    model.Errors,
    e => e.Code == ErrorCode.Gen_UnrecognizedParameters);
    }

    [Fact]
    public void Parse_InvalidTime_AddsError()
    {
      var parser = new PtCommandParser();

      var model = Assert.IsType<PtCommandModel>(
          parser.Parse(
              "10",
              "ПТ",
              1,
              new List<string>
              {
                "10 ПТ 1000ч"
              }));

      Assert.Contains(
    model.Errors,
    e => e.Code == ErrorCode.Gen_UnrecognizedParameters);
    }
  }
}
