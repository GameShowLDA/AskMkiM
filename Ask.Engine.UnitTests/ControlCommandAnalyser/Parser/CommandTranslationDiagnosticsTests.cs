using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Diagnostics;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;

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
  public void GetKnownCommandKeysByMnemonic_UsesCommandModels()
  {
    var keysByMnemonic = CommandTranslationManager.GetKnownCommandKeysByMnemonic();

    Assert.Contains("ЦУ", keysByMnemonic.Keys);
    Assert.Contains(AlgorithmKey.Д, keysByMnemonic["ЦУ"]);
    Assert.Contains("ОК", keysByMnemonic.Keys);
    Assert.Empty(keysByMnemonic["ОК"]);
  }

  [Fact]
  public void ParseForDiagnostics_FillsCommandMetadata()
  {
    var manager = new CommandTranslationManager();

    var model = Assert.Single(manager.ParseForDiagnostics("1 ЦУ Д Документ"));

    Assert.True(model.IsCommandMnemonic("цу"));
    Assert.True(model.AllowsAlgorithmKey(AlgorithmKey.Д));
    Assert.False(model.AllowsAlgorithmKey(AlgorithmKey.К));
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

  [Fact]
  public void ParseForDiagnostics_ContinuesAfterUnknownCommand()
  {
    var manager = new CommandTranslationManager();

    var models = manager.ParseForDiagnostics(
      "1 НЕИЗВЕСТНО текст" + Environment.NewLine +
      "2 ЦУ Продолжить? да");

    Assert.Equal(2, models.Count);
    Assert.Contains(models[0].Errors, error => error.Code == ErrorCode.Gen_UnknownCommand);
    Assert.Contains(
      models[1].Warnings,
      warning => warning.Description.Contains("Вопросительный знак"));
  }

  [Theory]
  [InlineData("1 КС 10<Ом<15 *1.1.1-1.1.2*")]
  [InlineData("1 ПР Ом<15 *1.1.1-1.1.2*")]
  [InlineData("1 ИЕ 1пФ<2пФ *1.1.1-1.1.2*")]
  [InlineData("1 СИ 100В 1МОм *1.1.1-1.1.2*")]
  [InlineData("1 ПИ 100В 1МОм *1.1.1-1.1.2*")]
  [InlineData("1 ВШ *2Ш:999*")]
  [InlineData("1 РМ X1/1=1.2.1")]
  public void ParseForDiagnostics_DoesNotProduceEquipmentIssues(string text)
  {
    var manager = new CommandTranslationManager();

    var issues = manager.ParseForDiagnostics(text)
      .SelectMany(model => model.Errors.Cast<IDisplayIssue>().Concat(model.Warnings));

    Assert.DoesNotContain(issues, TranslationDiagnosticClassifier.IsEquipmentRelated);
  }

  [Fact]
  public void ParseForDiagnostics_MultilinePrWithComment_RecognizesKeyBeforePoints()
  {
    const string text =
      "200 ПР 15<Ом, И, Г, ЗС  {проверка И,Г,ЗС}\n" +
      "\t*К1/31-35*К1/41-45*К1/51-55,К1/61-65*К1/71-75";
    var manager = new CommandTranslationManager();

    var model = Assert.IsType<PrCommandModel>(
      Assert.Single(manager.ParseForDiagnostics(text)));

    Assert.Contains("ЗС", model.AlgorithmKey);
    Assert.DoesNotContain(
      model.Errors,
      error => error.Code == ErrorCode.Gen_UnrecognizedParameters &&
               error.Description.Contains("ЗС"));
  }

  [Fact]
  public void EquipmentDiagnosticClassifier_RecognizesEquipmentCodesAndMessages()
  {
    Assert.True(TranslationDiagnosticClassifier.IsEquipmentRelated(new ErrorItem
    {
      Code = ErrorCode.Gen_FastMeterNotFound,
      Description = "Не найден быстрый измеритель."
    }));

    Assert.True(TranslationDiagnosticClassifier.IsEquipmentRelated(new ErrorItem
    {
      Code = ErrorCode.Rm_CannotParseExpression,
      Description = "MachineAddressNotConfigured: модуль отсутствует в конфигурации."
    }));

    Assert.True(TranslationDiagnosticClassifier.IsEquipmentRelated(new WarningItem
    {
      Code = WarningCode.Equipment_ChassisNotFound,
      Description = "Шасси не найдено."
    }));
  }

  [Fact]
  public void EquipmentDiagnosticClassifier_KeepsTextDiagnostics()
  {
    Assert.False(TranslationDiagnosticClassifier.IsEquipmentRelated(new ErrorItem
    {
      Code = ErrorCode.Gen_VoltageConflict,
      Description = "Напряжение не указано."
    }));

    Assert.False(TranslationDiagnosticClassifier.IsEquipmentRelated(new ErrorItem
    {
      Code = ErrorCode.Gen_InvalidRange,
      Description = "Неверное начало диапазона."
    }));
  }
}
