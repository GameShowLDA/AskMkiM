using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Translator;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using Ask.Engine.ControlCommandAnalyser.Parser;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser;

public sealed class VshSourceSuggestionTests
{
  [Theory]
  [InlineData("10", "11")]
  [InlineData("15", "16")]
  [InlineData("30", "31")]
  public void SourceAnalysisUsesNextNumberAfterRm(string rmNumber, string vshNumber)
  {
    string text = $"{rmNumber} РМ K1/1-100=1.6.1-100";
    var manager = new CommandTranslationManager(new[] { new StubRmParser() });

    var diagnostic = Assert.Single(
      manager.AnalyzeSource(text),
      item => item.Code == "Gen_VshCommandAddedAutomatically");

    Assert.NotNull(diagnostic.Suggestion);
    Assert.Equal($"{vshNumber} ВШ 2Ш", diagnostic.Suggestion.PreviewText);
    Assert.Equal(Environment.NewLine + $"{vshNumber} ВШ 2Ш", diagnostic.Suggestion.Text);
  }

  [Fact]
  public void SuggestionUsesFirstBlankLineBetweenRmAndNextCommand()
  {
    const string text =
      "30 РМ K1/1-100=1.6.1-100\r\n" +
      "      K4/1-100=1.8.1-100\r\n" +
      "\r\n" +
      "40 ЦУ Текст";
    var rm = new RmCommandModel { CommandNumber = "30", StartLineNumber = 1 };
    var next = new CuCommandModel { CommandNumber = "40", StartLineNumber = 4 };
    var vsh = new VshCommandModel
    {
      CommandNumber = "31",
      SourceLines = new List<string> { "31 ВШ 2Ш" },
    };

    var suggestion = CommandTranslationManager.CreateVshSourceSuggestion(
      text,
      new BaseCommandModel[] { rm, next },
      rm,
      vsh);

    int expectedOffset = text.IndexOf("\r\n\r\n", StringComparison.Ordinal) + 2;
    Assert.Equal(expectedOffset, suggestion.Offset);
    Assert.Equal("31 ВШ 2Ш\r\n", suggestion.Text);
    Assert.Equal("31 ВШ 2Ш", suggestion.PreviewText);
    Assert.False(suggestion.PreviewOnNextLine);
  }

  [Fact]
  public void SuggestionAddsNewLineWhenRmEndsDocument()
  {
    const string text = "30 РМ K1/1-100=1.6.1-100";
    var rm = new RmCommandModel { CommandNumber = "30", StartLineNumber = 1 };
    var vsh = new VshCommandModel
    {
      CommandNumber = "31",
      SourceLines = new List<string> { "31 ВШ 2Ш" },
    };

    var suggestion = CommandTranslationManager.CreateVshSourceSuggestion(
      text,
      new BaseCommandModel[] { rm },
      rm,
      vsh);

    Assert.Equal(text.Length, suggestion.Offset);
    Assert.Equal(Environment.NewLine + "31 ВШ 2Ш", suggestion.Text);
    Assert.True(suggestion.PreviewOnNextLine);
  }

  [Theory]
  [InlineData("180 ПР Ом<5 *К1/11*", "180 ПР ЗР Ом<5 *К1/11*")]
  [InlineData("  180\tПР\tОм<5 *К1/11*", "  180\tПР ЗР\tОм<5 *К1/11*")]
  [InlineData("180 ПР", "180 ПР ЗР")]
  public void PrAlgorithmKeySuggestionIsInsertedAfterMnemonic(string source, string expected)
  {
    var model = new PrCommandModel { CommandNumber = "180", StartLineNumber = 1 };
    var lines = new List<(int Offset, string Text)> { (0, source) };

    var suggestion = CommandTranslationManager.CreatePrAlgorithmKeySuggestion(lines, model, "ЗР");

    Assert.NotNull(suggestion);
    Assert.Equal(expected, source.Insert(suggestion.Offset, suggestion.Text));
    Assert.Equal(" ЗР", suggestion.PreviewText);
  }

  [Fact]
  public void SourceAnalysisAttachesOwnKeySuggestionToEveryPrWarning()
  {
    const string text = "180 ПР Ом<5 *К1/11*\r\n190 ПР Ом<5 *К1/12*";
    var manager = new CommandTranslationManager(new[] { new StubPrParser() });

    var diagnostics = manager.AnalyzeSource(text)
      .Where(item => item.Code == "Gen_KeyZR")
      .ToArray();

    Assert.Equal(2, diagnostics.Length);
    Assert.All(diagnostics, diagnostic => Assert.NotNull(diagnostic.Suggestion));
    Assert.Equal(text.IndexOf("ПР", StringComparison.Ordinal) + "ПР".Length, diagnostics[0].Suggestion!.Offset);
    Assert.Equal(text.LastIndexOf("ПР", StringComparison.Ordinal) + "ПР".Length, diagnostics[1].Suggestion!.Offset);
  }

  private sealed class StubRmParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic) => mnemonic.Mnemonic == "РМ";

    public BaseCommandModel Parse(
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines) => new RmCommandModel
      {
        CommandNumber = commandNumber,
        StartLineNumber = numberLine,
        SourceLines = lines,
      };
  }

  private sealed class StubPrParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic) => mnemonic.Mnemonic == "ПР";

    public BaseCommandModel Parse(
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines)
    {
      var model = new PrCommandModel
      {
        CommandNumber = commandNumber,
        StartLineNumber = numberLine,
        SourceLines = lines,
      };
      model.Warnings.Add(GeneralWarnings.KeyZR(numberLine, $"{commandNumber} {mnemonic}"));
      return model;
    }
  }
}
