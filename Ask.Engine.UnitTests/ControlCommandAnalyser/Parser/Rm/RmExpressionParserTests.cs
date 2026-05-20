using Ask.Core.Services.Errors.Models;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Rm;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser.Rm;

public class RmExpressionParserTests
{
  [Fact(DisplayName = "РМ парсер: диапазоны букв после косой черты с числовой границей разворачиваются полностью")]
  public void ParseAllExpressions_WithSlashLetterRanges_ExpandsAllPairsWithoutErrors()
  {
    var model = CreateModel();
    string input = """
      X1/a1-56=1.1.1-1.1.56
      X1/b1-56=1.2.1-1.2.56
      X1/c1-44=1.2.57-1.2.100
      X1/c45-56=1.3.1-1.3.12
      X1/d1-56=1.3.13-1.3.68
      """;

    var pairs = RmExpressionParser.ParseAllExpressions(input, ref model);

    Assert.Empty(model.Errors);
    Assert.Equal(224, pairs.Count);
    Assert.Equal("X1/a1", pairs[0].OkPoint);
    Assert.Equal("1.1.1", pairs[0].AskInput);
    Assert.Equal("X1/d56", pairs[^1].OkPoint);
    Assert.Equal("1.3.68", pairs[^1].AskInput);
  }

  [Fact(DisplayName = "РМ парсер: поддерживаются строчные буквенные диапазоны после косой черты")]
  public void ExpandAll_WithLowercaseLetterRangeAndNumericEnd_ExpandsRange()
  {
    var model = CreateModel();

    var expanded = RmExpressionParser.ExpandAll("X1/a1-3", ref model);

    Assert.Empty(model.Errors);
    Assert.Equal(new[] { "X1/a1", "X1/a2", "X1/a3" }, expanded);
  }

  [Fact(DisplayName = "РМ модель: адрес точки находится после нормализации мнемоники")]
  public void TryGetAddressByKey_WithHomoglyphLetters_FindsMappedAddress()
  {
    var rm = new RmCommandModel
    {
      PointsMap = new Dictionary<string, string>
      {
        ["Х1/а11"] = "1.1.11"
      }
    };

    var found = rm.TryGetAddressByKey("X1/a11", out var address);

    Assert.True(found);
    Assert.Equal("1.1.11", address);
  }

  [Fact(DisplayName = "РМ парсер: синонимы разворачиваются вместе с диапазонами ОК и АСК")]
  public void ParseAllExpressions_WithSynonymRanges_ExpandsTriples()
  {
    var model = CreateModel();

    var pairs = RmExpressionParser.ParseAllExpressions("A1-A3==S1-S3=1.1.1-1.1.3", ref model);

    Assert.Empty(model.Errors);
    Assert.Equal(new[] { "A1", "A2", "A3" }, pairs.Select(pair => pair.OkPoint).ToArray());
    Assert.Equal(new[] { "S1", "S2", "S3" }, pairs.Select(pair => pair.Synonym).ToArray());
    Assert.Equal(new[] { "1.1.1", "1.1.2", "1.1.3" }, pairs.Select(pair => pair.AskInput).ToArray());
  }

  [Fact(DisplayName = "РМ парсер: квадратные скобки в точках АСК разворачиваются декартовым произведением")]
  public void ParseAllExpressions_WithBracketProduct_ExpandsCartesianProduct()
  {
    var model = CreateModel();

    var pairs = RmExpressionParser.ParseAllExpressions("101-104=1.[3,5].1-2", ref model);

    Assert.Empty(model.Errors);
    Assert.Equal(new[] { "101", "102", "103", "104" }, pairs.Select(pair => pair.OkPoint).ToArray());
    Assert.Equal(new[] { "1.3.1", "1.3.2", "1.5.1", "1.5.2" }, pairs.Select(pair => pair.AskInput).ToArray());
  }

  [Fact(DisplayName = "РМ парсер: диапазоны синонимов через двоеточие и шаги АСК разворачиваются корректно")]
  public void ParseAllExpressions_WithColonSynonymAndStep_ExpandsRanges()
  {
    var model = CreateModel();

    var pairs = RmExpressionParser.ParseAllExpressions("301-303==XS1:1-3=1.6.2-6(2)", ref model);

    Assert.Empty(model.Errors);
    Assert.Equal(new[] { "301", "302", "303" }, pairs.Select(pair => pair.OkPoint).ToArray());
    Assert.Equal(new[] { "XS1:1", "XS1:2", "XS1:3" }, pairs.Select(pair => pair.Synonym).ToArray());
    Assert.Equal(new[] { "1.6.2", "1.6.4", "1.6.6" }, pairs.Select(pair => pair.AskInput).ToArray());
  }

  [Fact(DisplayName = "РМ парсер: перечисления через запятую разворачиваются с обеих сторон")]
  public void ParseAllExpressions_WithCommaEnumeration_MapsInOrder()
  {
    var model = CreateModel();

    var pairs = RmExpressionParser.ParseAllExpressions("A1,A3=1.1.1,1.1.3", ref model);

    Assert.Empty(model.Errors);
    Assert.Equal(new[] { "A1", "A3" }, pairs.Select(pair => pair.OkPoint).ToArray());
    Assert.Equal(new[] { "1.1.1", "1.1.3" }, pairs.Select(pair => pair.AskInput).ToArray());
  }

  [Fact(DisplayName = "РМ парсер: поддерживаются убывающие диапазоны")]
  public void ParseAllExpressions_WithDescendingRanges_MapsInDescendingOrder()
  {
    var model = CreateModel();

    var pairs = RmExpressionParser.ParseAllExpressions("A3-A1=1.1.3-1.1.1", ref model);

    Assert.Empty(model.Errors);
    Assert.Equal(new[] { "A3", "A2", "A1" }, pairs.Select(pair => pair.OkPoint).ToArray());
    Assert.Equal(new[] { "1.1.3", "1.1.2", "1.1.1" }, pairs.Select(pair => pair.AskInput).ToArray());
  }

  [Fact(DisplayName = "РМ парсер: несовпадение количества точек ОК и АСК даёт ошибку")]
  public void ParseAllExpressions_WithMismatchedCounts_AddsError()
  {
    var model = CreateModel();

    var pairs = RmExpressionParser.ParseAllExpressions("A1-A2=1.1.1-1.1.3", ref model);

    Assert.Empty(pairs);
    AssertErrorCodes(model, ErrorCode.Rm_MismatchedCounts);
  }

  [Theory(DisplayName = "РМ парсер: недопустимые символы дают ошибку")]
  [InlineData("A1=\"1.1.1\"")]
  [InlineData("A$1=1.1.1")]
  public void ParseAllExpressions_WithUnacceptableSymbols_AddsError(string input)
  {
    var model = CreateModel();

    var pairs = RmExpressionParser.ParseAllExpressions(input, ref model);

    Assert.Empty(pairs);
    AssertErrorCodes(model, ErrorCode.Rm_UnacceptableSymbol);
  }

  [Fact(DisplayName = "РМ парсер: несколько выражений в одной строке разбираются отдельно")]
  public void SplitExpressions_WithSeveralExpressionsOnOneLine_ParsesAll()
  {
    var model = CreateModel();

    var pairs = RmExpressionParser.ParseAllExpressions("A1 == S1 = 1.1.1 A2=1.1.2", ref model);

    Assert.Empty(model.Errors);
    Assert.Equal(2, pairs.Count);
    Assert.Equal("S1", pairs[0].Synonym);
    Assert.Equal("A2", pairs[1].OkPoint);
  }

  [Fact(DisplayName = "РМ парсер: пустое тело команды даёт ошибку")]
  public void ParseAllExpressions_WithEmptyBody_AddsError()
  {
    var model = CreateModel();

    var pairs = RmExpressionParser.ParseAllExpressions("   ", ref model);

    Assert.Empty(pairs);
    AssertErrorCodes(model, ErrorCode.Rm_EmptyCommandBody);
  }

  private static RmCommandModel CreateModel()
  {
    return new RmCommandModel
    {
      CommandNumber = "30",
      StartLineNumber = 30
    };
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
