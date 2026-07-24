using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser
{
  public class CommentsParserTests
  {
    [Fact(DisplayName = "PreprocessText разбивает многострочный комментарий по строкам")]
    public void PreprocessTextAndExtractComments_SplitsMultilineCommentIntoSeparateEntries()
    {
      const string source = "1 ОК OBJ\n{  AAA\n    BBB\n    CCC\n}\n2 УП 10";

      var (_, comments) = PreprocessText.PreprocessTextAndExtractComments(source);

      Assert.Equal(
        new[] { "{  AAA", "    BBB", "    CCC", "}" },
        comments.Select(comment => comment.Text));
      Assert.Equal(
        new[] { 1, 2, 3, 4 },
        comments.Select(comment => comment.LineIndex));
    }

    [Fact(DisplayName = "CommentsParser сохраняет многострочный фигурный комментарий как отдельные строки")]
    public void ParseComments_SavesMultilineBraceCommentAsSeparateLines()
    {
      var model = new OkCommandModel();
      var lines = new List<string>
      {
        "1 ОК OBJ",
        "{  AAA",
        "    BBB",
        "    CCC",
        "}",
        "ПРИМ=TEXT"
      };

      var processedLines = CommentsParser.ParseComments(lines, model);

      Assert.Equal(
        new[] { "{  AAA", "    BBB", "    CCC", "}" },
        model.Comment);
      Assert.Equal(
        new[] { "1 ОК OBJ", "ПРИМ=TEXT" },
        processedLines);
    }
  }
}
