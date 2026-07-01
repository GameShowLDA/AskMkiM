using Ask.Engine.ControlCommandAnalyser;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser;

public sealed class CommandTranslationDiagnosticsTests
{
  [Fact]
  public void GetKnownCommandMnemonics_IncludesParserBackedDisplayNames()
  {
    var mnemonics = CommandTranslationManager.GetKnownCommandMnemonics();

    Assert.Contains("ЦУ", mnemonics);
    Assert.Contains("КС", mnemonics);
    Assert.Contains("ЭТ", mnemonics);
    Assert.Contains("НЭ", mnemonics);
  }

  [Fact]
  public void ParseForDiagnostics_WithEmptyCuMessage_AddsCommandError()
  {
    var manager = new CommandTranslationManager();

    var models = manager.ParseForDiagnostics("1 ЦУ");
    var errors = models.SelectMany(model => model.Errors);

    Assert.Contains(
      errors,
      error => error.Description.Contains("После команды ЦУ должен быть указан текст сообщения."));
  }

  [Fact]
  public void ParseForDiagnostics_WithCuQuestionInsideMessage_AddsCommandWarning()
  {
    var manager = new CommandTranslationManager();

    var models = manager.ParseForDiagnostics("1 ЦУ Продолжить? да");
    var warnings = models.SelectMany(model => model.Warnings);

    Assert.Contains(
      warnings,
      warning => warning.Description.Contains("Вопросительный знак в команде ЦУ должен завершать сообщение."));
  }
}
