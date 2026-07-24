using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Formatter.Base;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Formatter
{
  public class CommandFormatterCommentsTests
  {
    [Fact(DisplayName = "FormatComments выравнивает многострочный фигурный комментарий относительно первой строки")]
    public void FormatComments_AlignsMultilineBraceComment()
    {
      var model = new TestCommandModel
      {
        Comment =
        {
          "{  AAA",
          "    BBB",
          "      CCC",
          "           }"
        }
      };

      var lines = TestFormatter.FormatAll(model).ToList();

      Assert.Equal("\tКомментарии:", lines[0]);
      Assert.Equal("\t\t{  AAA", lines[1]);
      Assert.Equal("\t\t   BBB", lines[2]);
      Assert.Equal("\t\t     CCC", lines[3]);
      Assert.Equal("\t\t          }", lines[4]);
    }

    private sealed class TestFormatter : CommandFormatter<TestCommandModel>
    {
      public static IEnumerable<string> FormatAll(TestCommandModel model)
      {
        return FormatComments(model);
      }

      protected override IEnumerable<string> Format(TestCommandModel model)
      {
        return Enumerable.Empty<string>();
      }
    }

    private sealed class TestCommandModel : BaseCommandModel
    {
      public override string Mnemonic { get; set; } = "TST";
    }
  }
}
