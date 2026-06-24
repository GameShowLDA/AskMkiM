using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Models;
using Ask.Engine.ControlCommandAnalyser.Formatter;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Rm;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser.Rm;

public class RmCommandParserTests : IDisposable
{
  private readonly RmCommandParser parser = new();

  public RmCommandParserTests()
  {
    ExecutionConfig.SetLegacyCompatibilityMode(false);
  }

  [Fact(DisplayName = "РМ парсер команд: простая точка ОК сопоставляется с точкой АСК")]
  public void Parse_WithFlatExpression_FillsPointsMap()
  {
    var model = ParseLines("30 РМ X1/1=1.2.1");

    AssertErrorCodes(model);
    Assert.Single(model.PointsMap);
    Assert.Equal("1.2.1", model.PointsMap["X1/1"]);
    Assert.Empty(model.SynonymMap);
    Assert.Single(model.Pairs);
  }

  [Fact(DisplayName = "РМ парсер команд: синонимы сохраняются отдельно и используются при поиске адреса")]
  public void Parse_WithSynonymExpression_FillsSynonymMap()
  {
    var model = ParseLines("30 РМ X1/1==S1=1.2.1");

    AssertErrorCodes(model);
    Assert.Equal("1.2.1", model.PointsMap["X1/1"]);
    Assert.Equal("1.2.1", model.SynonymMap["S1"]);
    Assert.True(model.TryGetAddressByKey("S1", out var address));
    Assert.Equal("1.2.1", address);
    Assert.Single(model.GetAllDestinationPoints());
  }

  [Fact(DisplayName = "РМ парсер команд: нумерованные части разбираются в модель")]
  public void Parse_WithNumberedParts_FillsPartsAndPairs()
  {
    var model = ParseLines(
      "30 РМ",
      "* Ч=1",
      "X1/1==S1=1.2.1",
      "*",
      "Ч=2",
      "X2/1=1.4.1",
      "*");

    AssertErrorCodes(model);
    Assert.Equal(2, model.Parts.Count);
    Assert.Equal(new int?[] { 1, 2 }, model.Parts.Select(part => part.PartNumber).ToArray());
    Assert.Equal(new[] { "X1/1", "X2/1" }, model.Pairs.Select(pair => pair.OkPoint).ToArray());
    Assert.Equal(1, model.Pairs[0].PartNumber);
    Assert.Equal(2, model.Pairs[1].PartNumber);
    Assert.Equal("1.2.1", model.SynonymMap["S1"]);
  }

  [Fact(DisplayName = "РМ парсер команд: полное тело команды сохраняется в источнике точек")]
  public void Parse_WithSeveralBodyLines_KeepsFullSource()
  {
    var model = ParseLines(
      "30 РМ X1/1=1.2.1",
      "X2/1=1.4.1");

    AssertErrorCodes(model);
    Assert.Contains("X1/1=1.2.1", model.PointsSourse);
    Assert.Contains("X2/1=1.4.1", model.PointsSourse);
  }

  [Fact(DisplayName = "РМ парсер команд: диапазоны разворачиваются в словари модели")]
  public void Parse_WithExpandedExpression_FillsAllMapsInOrder()
  {
    var model = ParseLines("30 РМ A1-A3==S1-S3=1.2.1-1.2.3");

    AssertErrorCodes(model);
    Assert.Equal(new[] { "A1", "A2", "A3" }, model.PointsMap.Keys.ToArray());
    Assert.Equal(new[] { "S1", "S2", "S3" }, model.SynonymMap.Keys.ToArray());
    Assert.Equal(new[] { "1.2.1", "1.2.2", "1.2.3" }, model.PointsMap.Values.ToArray());
  }

  [Fact(DisplayName = "РМ парсер команд: дублирование точки ОК даёт ошибку")]
  public void Parse_WithDuplicateOkPoint_AddsErrorAndKeepsFirstMapping()
  {
    var model = ParseLines(
      "30 РМ X1/1=1.2.1",
      "X1/1=1.2.2");

    AssertErrorCodes(model, ErrorCode.Rm_CannotParseExpression);
    Assert.Single(model.PointsMap);
    Assert.Equal("1.2.1", model.PointsMap["X1/1"]);
  }

  [Fact(DisplayName = "РМ парсер команд: дублирование синонима даёт ошибку")]
  public void Parse_WithDuplicateSynonym_AddsError()
  {
    var model = ParseLines(
      "30 РМ X1/1==S1=1.2.1",
      "X2/1==S1=1.2.2");

    AssertErrorCodes(model, ErrorCode.Rm_CannotParseExpression);
    Assert.Equal("1.2.1", model.SynonymMap["S1"]);
    Assert.Equal(2, model.PointsMap.Count);
  }

  [Fact(DisplayName = "РМ парсер команд: некорректный формат точки АСК даёт ошибку")]
  public void Parse_WithInvalidAskPoint_AddsErrorAndDoesNotMapPair()
  {
    var model = ParseLines("30 РМ X1/1=bad");

    AssertErrorCodes(model, ErrorCode.Rm_CannotParseExpression);
    Assert.Empty(model.PointsMap);
    Assert.Empty(model.Pairs);
  }

  [Fact(DisplayName = "РМ парсер команд: отсутствие номера части после разделителя даёт ошибку")]
  public void Parse_WithPartSeparatorWithoutNumber_AddsError()
  {
    var model = ParseLines(
      "30 РМ",
      "*",
      "X1/1=1.2.1",
      "*");

    AssertErrorCodes(model, ErrorCode.Rm_CannotParseExpression);
    Assert.Equal("1.2.1", model.PointsMap["X1/1"]);
  }

  [Fact(DisplayName = "РМ форматтер: части и синонимы сохраняются при форматировании")]
  public void Format_WithPartsAndSynonyms_OutputsStructuredCommand()
  {
    var model = ParseLines(
      "30 РМ",
      "* Ч=1",
      "X1/1==S1=1.2.1",
      "*",
      "Ч=2",
      "X2/1=1.4.1");

    AssertErrorCodes(model);
    var lines = new RmCommandFormatter().Format(model).ToArray();

    Assert.Contains("30 РМ", lines);
    Assert.Contains("\t* Ч=1", lines);
    Assert.Contains("\tX1/1 == S1 = 1.2.1", lines);
    Assert.Contains("\t* Ч=2", lines);
    Assert.Contains("\tX2/1 = 1.4.1", lines);
  }

  [Fact(DisplayName = "РМ форматтер: в режиме совместимости выводит старый и реальный адрес")]
  public void Format_WithLegacyCompatibilityMode_OutputsLegacyAndRealAddress()
  {
    ExecutionConfig.SetLegacyCompatibilityMode(true);
    var model = new RmCommandModel
    {
      CommandNumber = "10",
      Pairs =
      {
        new RmPairModel
        {
          OkPoint = "хр1/1",
          AskInput = "1.2.1",
          LegacyAskInput = "1.1.1"
        }
      },
      PointsMap =
      {
        ["хр1/1"] = "1.2.1"
      }
    };
    model.Parts.Add(new RmPartModel { Pairs = { model.Pairs[0] } });

    var lines = new RmCommandFormatter().Format(model).ToArray();

    Assert.Contains("\tхр1/1 = 1.1.1(1.2.1)", lines);
  }

  public void Dispose()
  {
    ExecutionConfig.SetLegacyCompatibilityMode(false);
  }

  private RmCommandModel ParseLines(params string[] lines)
  {
    return Assert.IsType<RmCommandModel>(parser.Parse("30", "РМ", 30, lines.ToList()));
  }

  private static void AssertErrorCodes(RmCommandModel model, params ErrorCode[] expectedCodes)
  {
    var actualCodes = model.Errors
      .Select(error => error.Code!.Value)
      .OrderBy(code => code.ToString())
      .ToArray();

    var expected = expectedCodes
      .OrderBy(code => code.ToString())
      .ToArray();

    Assert.Equal(expected, actualCodes);
  }
}
