using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser
{
  public class CommentsParserTests
  {
    private const string NestedProgram = "180 ПР ЗР Ом<5 *К1/11,К1/51-55*\n"
      + " {190 ПР\n *К1/31-35*К1/41-45*К1/51-55,К1/61-65\n"
      + " *К1/71-75*К1/81-85*К1/91-95*\n"
      + "200 ПР 15<Ом, И, Г, ЗС {проверка И, Г,ЗС}\n"
      + " *К1/31-35*К1/41-45*К1/51-55*К1/61-65*К1/71-75\n"
      + " *К1/81-85*К1/91-95#К1/1*\n"
      + " *К1/11-15*К1/21-25* {проверка П}\n210 ПР П\n }\n"
      + "215 ПР ЗР ОМ<5 *К1/64, К1/65*";

    [Fact]
    public void NestedProgram_KeepsOnlyCommandsOutsideOuterComment()
    {
      var (clean, comments) = PreprocessText.PreprocessTextAndExtractComments(NestedProgram);
      var expected = new[]
      {
        "180 ПР ЗР Ом<5 *К1/11,К1/51-55*",
        "215 ПР ЗР ОМ<5 *К1/64, К1/65*"
      };
      Assert.Equal(expected, clean.Values.Select(line => line.Trim()));
      Assert.Contains(comments, comment => comment.Text == "210 ПР П");

      var model = new OkCommandModel();
      var parsed = CommentsParser.ParseComments(NestedProgram.Split('\n').ToList(), model);
      Assert.Equal(expected, parsed.Select(line => line.Trim()));
      Assert.Equal(comments.Select(comment => comment.Text), model.Comment);

      using var scope = CommandsModel.BeginAnalysisScope();
      var manager = new CommandTranslationManager(Array.Empty<ICommandParser>());
      var commands = manager.ParseAll(NestedProgram, emitMessages: false);
      Assert.Equal(new[] { "180", "215" }, commands.Select(command => command.CommandNumber));
      Assert.Equal(new[] { 1, 11 }, commands.Select(command => command.StartLineNumber));
    }

    [Theory]
    [InlineData("{", "}")]
    [InlineData("/*", "*/")]
    public void NestedMixedComments_LineCommentDelimitersDoNotCloseBlocks(string open, string close)
    {
      var source = $"10 ОК OBJ\n{open}\n{{nested /* deep */}}\n"
        + "// } */ { /* ignored until newline\n210 ПР П\n"
        + $"{close}\n215 ПР П";
      var (clean, comments) = PreprocessText.PreprocessTextAndExtractComments(source);
      Assert.Equal(new[] { "10 ОК OBJ", "215 ПР П" }, clean.Values);
      Assert.Contains(comments, comment => comment.Text == "210 ПР П");
    }

    [Fact]
    public void UnclosedNestedComment_ReportsOuterOpeningAndHidesRemainingCommands()
    {
      var openings = new List<(int, int)>();
      var (clean, _) = PreprocessText.PreprocessTextAndExtractComments(
        "10 ОК OBJ\n { /* nested */\n210 ПР П", (line, column) => openings.Add((line, column)));
      Assert.Equal(new[] { "10 ОК OBJ" }, clean.Values);
      Assert.Equal(new[] { (1, 1) }, openings);
    }

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
