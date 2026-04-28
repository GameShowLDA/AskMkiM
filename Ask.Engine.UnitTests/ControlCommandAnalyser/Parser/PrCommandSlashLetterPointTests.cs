using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Services.Errors.Models;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using Ask.Engine.ControlCommandAnalyser.Parser;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Rm;
using System.Text.RegularExpressions;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser;

public class PrCommandSlashLetterPointTests : IDisposable
{
  [Fact(DisplayName = "Key parser does not remove contact letter c from PR points")]
  public void ParseKeys_WithKeyCAndSlashLetterPoints_KeepsContactLetterC()
  {
    var pr = new PrCommandModel
    {
      CommandNumber = "70",
      StartLineNumber = 70
    };
    const string remainder = "Ом<15, С *Х1/с25,Х1/с26,Х1/с27*";

    var parsed = KeyParser.ParseKeys(70, pr, remainder);

    Assert.Contains("С", pr.AlgorithmKey);
    Assert.Contains("Х1/с25", parsed);
    Assert.Contains("Х1/с26", parsed);
    Assert.Contains("Х1/с27", parsed);
    Assert.DoesNotContain(", С ", parsed);
  }

  [Fact(DisplayName = "PR command keeps slash contact letters when key C adds RM points")]
  public void BuildTranslation_WithSlashLetterPointsAndKeyC_DoesNotReportPointsWithoutContactLetter()
  {
    var rm = CreateRmCommand();
    CommandsModel.CommandModels.Add(rm);

    var pr = new PrCommandModel
    {
      CommandNumber = "70",
      StartLineNumber = 70,
      AlgorithmKey = { "С" }
    };
    var lines = new List<string>
    {
      "70 ПР  Ом<15, С",
      "    *Х1/а11,Х1/b6,Х1/а6,Х1/b11,",
      "     Х1/а52,Х1/b52,Х1/а50,Х1/b50 {Еп+}",
      "    *Х1/а25,Х1/а26,Х1/а27,",
      "     Х1/с25,Х1/с26,Х1/с27        {корпус1}",
      "    *Х1/а30,Х1/а31,Х1/а32,",
      "     Х1/с28,Х1/с29,Х1/с30        {корпус2}",
      "    *Х1/а2,Х1/b2,Х1/а7,Х1/b7,",
      "     Х1/а51,Х1/b51,Х1/а49,Х1/b49 {Еп-}",
      "    *Х1/а4,Х1/b4,Х1/а54,Х1/b54   {обтекание1+}",
      "    *Х1/а3,Х1/b3,Х1/а53,Х1/b53   {обтекание1-}",
      "    *Х1/а56,Х1/b56,Х1/а5,Х1/b5   {дистан. стирание+}",
      "    *Х1/с14,Х1/d14,Х1/с24,Х1/d24 {обтекание2+}",
      "    *Х1/с3,Х1/d3,Х1/с17,Х1/d17   {обтекание2-}",
      "    *Х1/с56,Х1/d56,Х1/с5,Х1/d5   {ДСВ+}",
      "    *Х1/с55,Х1/d55,Х1/с6,Х1/d6*  {ВКЛ+}"
    };
    pr.SourceLines = new List<string>(lines);
    string remainder = PreprocessSourceLines.GetClearCommandBody(pr, lines);
    remainder = Regex.Match(remainder, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s*(.*)$").Groups[1].Value.Trim();

    pr.Scheme = SchemeManager.GetScheme(pr, rm, 70, ref remainder);

    var unknownPoints = pr.Errors
      .Where(error => error.Code == ErrorCode.Gen_UnknownPoint)
      .Select(error => error.Description)
      .ToArray();

    Assert.DoesNotContain(unknownPoints, description => description.Contains("X1/25"));
    Assert.DoesNotContain(unknownPoints, description => description.Contains("X1/26"));
    Assert.DoesNotContain(unknownPoints, description => description.Contains("X1/27"));
    Assert.DoesNotContain(unknownPoints, description => description.Contains("X1/28"));
    Assert.DoesNotContain(unknownPoints, description => description.Contains("X1/29"));
    Assert.Empty(unknownPoints);
  }

  public void Dispose()
  {
    Ask.Engine.ControlCommandAnalyser.Model.CommandsModel.Clear();
  }

  private static RmCommandModel CreateRmCommand()
  {
    var rm = new RmCommandModel
    {
      CommandNumber = "30",
      StartLineNumber = 30
    };

    string input = """
      Х1/а1-56=1.1.1-1.1.56
      Х1/b1-56=1.2.1-1.2.56
      Х1/с1-44=1.2.57-1.2.100
      Х1/с45-56=1.3.1-1.3.12
      Х1/d1-56=1.3.13-1.3.68
      """;

    foreach (var pair in RmExpressionParser.ParseAllExpressions(input, ref rm))
      rm.PointsMap[pair.OkPoint] = pair.AskInput;

    return rm;
  }
}
