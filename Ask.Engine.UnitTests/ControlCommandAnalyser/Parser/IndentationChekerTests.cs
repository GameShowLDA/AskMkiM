using Ask.Engine.ControlCommandAnalyser.Parser;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser;

public class IndentationChekerTests
{
  [Fact(DisplayName = "Проверка отступов: одиночный разделитель цепей ПР допускается без отступа")]
  public void CheckIndentationErrors_WithStandaloneChainSeparator_DoesNotReportError()
  {
    var lines = new List<string>
    {
      "60 ПР С",
      "        *1=XS1/2,XS3/2,XP15/2",
      "*"
    };

    var errors = IndentationCheker.CheckIndentationErrors(lines, "60", "ПР");

    Assert.Empty(errors);
  }

  [Fact(DisplayName = "Проверка отступов: строка продолжения без отступа по-прежнему даёт ошибку")]
  public void CheckIndentationErrors_WithContinuationLineWithoutIndentation_ReportsError()
  {
    var lines = new List<string>
    {
      "60 ПР С",
      "        *1=XS1/2,XS3/2,XP15/2",
      "2=XS1/3,XS3/3,XP15/3"
    };

    var errors = IndentationCheker.CheckIndentationErrors(lines, "60", "ПР");

    Assert.Single(errors);
  }
}
