using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using Ask.Engine.ControlCommandAnalyser.Parser.Rm;
using Ask.Engine.ControlCommandAnalyser.Parser;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser.Rm;

public class RmExpressionParserTests
{
  [Fact(DisplayName = "RM parser expands slash letter ranges with numeric end")]
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

  [Fact(DisplayName = "RM parser supports lowercase letter ranges after slash")]
  public void ExpandAll_WithLowercaseLetterRangeAndNumericEnd_ExpandsRange()
  {
    var model = CreateModel();

    var expanded = RmExpressionParser.ExpandAll("X1/a1-3", ref model);

    Assert.Empty(model.Errors);
    Assert.Equal(new[] { "X1/a1", "X1/a2", "X1/a3" }, expanded);
  }

  

  [Fact(DisplayName = "RM model finds point address after mnemonic normalization")]
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

  private static RmCommandModel CreateModel()
  {
    return new RmCommandModel
    {
      CommandNumber = "30",
      StartLineNumber = 30
    };
  }
}
