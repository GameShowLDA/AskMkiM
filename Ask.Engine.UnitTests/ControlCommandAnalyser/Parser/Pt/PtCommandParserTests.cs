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
      Assert.DoesNotContain(model.Errors, error => error.Code == ErrorCode.Gen_NoPointsBody);
    }

    [Fact]
    public void Parse_MillisecondsWithoutPoints_ParsesDelayWithoutPointsError()
    {
      var model = Parse("10 ПТ 10мс");

      Assert.Equal(0.01, model.Time);
      Assert.Empty(model.BusPointsDictionary);
      Assert.DoesNotContain(model.Errors, error => error.Code == ErrorCode.Gen_NoPointsBody);
    }

    [Theory]
    [InlineData("5мс", 0.005)]
    [InlineData("99990мс", 99.99)]
    [InlineData("0.005с", 0.005)]
    [InlineData("99.99с", 99.99)]
    public void Parse_TimeAtAllowedBoundaries_ParsesWithoutRangeError(string source, double expectedSeconds)
    {
      var model = Parse($"10 ПТ {source}");

      Assert.Equal(expectedSeconds, model.Time);
      Assert.DoesNotContain(model.Errors, error => error.Code == ErrorCode.Pt_TimeOutOfRange);
    }

    [Theory]
    [InlineData("4мс")]
    [InlineData("99991мс")]
    [InlineData("0.004с")]
    [InlineData("100с")]
    public void Parse_TimeOutsideAllowedBoundaries_AddsRangeError(string source)
    {
      var model = Parse($"10 ПТ {source}");

      Assert.Contains(model.Errors, error => error.Code == ErrorCode.Pt_TimeOutOfRange);
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
    public void Parse_WithPoints_KeepsBusConnectionParameters()
    {
      var model = Parse("10 ПТ *A1:X1*");

      Assert.Equal("*A1:X1*", model.PointsSourse);
      Assert.DoesNotContain(model.Errors, error => error.Code == ErrorCode.Gen_NoPointsBody);
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

    private static PtCommandModel Parse(string source)
    {
      return Assert.IsType<PtCommandModel>(new PtCommandParser().Parse(
        "10",
        "ПТ",
        1,
        new List<string> { source }));
    }
  }
}
