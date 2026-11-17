using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser;

public sealed class CommandNumberUniquenessTests
{
  [Fact]
  public void ParseAll_DifferentMnemonicsWithSameNumber_AddsErrorToSecondCommand()
  {
    const string source = "50 ПР П, Г\r\n50 ВИ *4*";
    var manager = new CommandTranslationManager(new[] { new StubCommandParser() });

    var models = manager.ParseAll(source, emitMessages: false);

    var duplicate = Assert.Single(models[1].Errors, error =>
      error.Code == ErrorCode.Gen_CommandAlreadyExists);
    Assert.Equal("50", duplicate.SourceFragment);

    var diagnostic = Assert.Single(
      CommandTranslationManager.MapDiagnostics(source, models, CancellationToken.None),
      item => item.Code == ErrorCode.Gen_CommandAlreadyExists.ToString());
    Assert.Equal("50", source.Substring(diagnostic.Offset, diagnostic.Length));
  }

  private sealed class StubCommandParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic) => true;

    public BaseCommandModel Parse(
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines) => new StubCommandModel
      {
        CommandNumber = commandNumber,
        Mnemonic = mnemonic,
        StartLineNumber = numberLine,
        SourceLines = lines,
      };
  }

  private sealed class StubCommandModel : BaseCommandModel;
}
