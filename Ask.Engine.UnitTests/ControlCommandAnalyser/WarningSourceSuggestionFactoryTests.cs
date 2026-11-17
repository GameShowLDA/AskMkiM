using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using Ask.Engine.ControlCommandAnalyser.Parser;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser;

public sealed class WarningSourceSuggestionFactoryTests
{
  [Fact]
  public void DuplicateKeySuggestionRemovesRepeatedKey()
  {
    const string source = "180 ПР ЗР, ЗР Ом<5 *К1/11*";
    var model = new PrCommandModel { StartLineNumber = 1 };
    var warning = GeneralWarnings.DuplicateKey(1, "180 ПР", "ЗР");

    var suggestion = Create(source, model, warning);

    Assert.Equal("180 ПР ЗР Ом<5 *К1/11*", Apply(source, suggestion));
  }

  [Theory]
  [InlineData(true, "180 ПР 10В, Ом<5 *К1/11*", "180 ПР Ом<5 *К1/11*")]
  [InlineData(false, "180 ПР 20мА, Ом<5 *К1/11*", "180 ПР Ом<5 *К1/11*")]
  public void IgnoredParameterSuggestionRemovesParameter(bool voltage, string source, string expected)
  {
    var model = new PrCommandModel { StartLineNumber = 1 };
    var warning = voltage
      ? GeneralWarnings.IgnoreVoltage(1, "180 ПР")
      : GeneralWarnings.IgnoreAmperage(1, "180 ПР");

    var suggestion = Create(source, model, warning);

    Assert.Equal(expected, Apply(source, suggestion));
  }

  [Fact]
  public void DefaultSiResistanceSuggestionAddsParsedValueBeforePoints()
  {
    const string source = "200 СИ 500В *К1/11*";
    var model = new SiCommandModel
    {
      StartLineNumber = 1,
      ResistanceSource = "100<МОм",
    };
    var warning = GeneralWarnings.DefaultResistainceLowLimit(1, "200 СИ", "100 МОм");

    var suggestion = Create(source, model, warning);

    Assert.Equal("200 СИ 500В, 100<МОм *К1/11*", Apply(source, suggestion));
  }

  [Fact]
  public void DefaultCapacitySuggestionCompletesRange()
  {
    const string source = "210 ИЕ 10<пф *К1/11*";
    var model = new IeCommandModel
    {
      StartLineNumber = 1,
      LowerLimitCapacity = 10,
      HigherLimitCapacity = 100,
      CapacityUnit = "пФ",
    };
    var warning = GeneralWarnings.DefaultCapacityHighLimit(1, "210 ИЕ", "100 пФ");

    var suggestion = Create(source, model, warning);

    Assert.Equal("210 ИЕ 10<пФ<100 *К1/11*", Apply(source, suggestion));
  }

  [Fact]
  public void DefaultTimeSuggestionAddsNumericValue()
  {
    const string source = "220 СИ с *К1/11*";
    var model = new SiCommandModel { StartLineNumber = 1 };
    var warning = GeneralWarnings.DefaultTime(1, "220 СИ", "1с");

    var suggestion = Create(source, model, warning);

    Assert.Equal("220 СИ 1с *К1/11*", Apply(source, suggestion));
  }

  [Fact]
  public void PeConversionSuggestionChangesMnemonicAndAddsResistance()
  {
    const string source = "230 ПЭ ЗС *К1/11*";
    var model = new PrCommandModel
    {
      StartLineNumber = 1,
      SourceLines = new List<string> { "230 ПЭ ЗС", " Ом>80", "*К1/11*" },
    };
    var warning = GeneralWarnings.PeToPrCommandAddedAutomatically(1, "230 ПР");

    var suggestion = Create(source, model, warning);

    Assert.Equal("230 ПР ЗС, Ом>80 *К1/11*", Apply(source, suggestion));
  }

  [Fact]
  public void CompletedPointRangeSuggestionWritesExplicitRightBoundary()
  {
    const string source = "240 КС *K1/A1-2*";
    var model = new KsCommandModel { StartLineNumber = 1 };
    var warning = new WarningItem
    {
      SourceLineNumber = 1,
      Command = "240 КС",
      Code = WarningCode.Unknown,
      Description = "Запись диапазона (K1/A1-2) была дополнена. Убедитесь в правильности записи.",
    };

    var suggestion = Create(source, model, warning);

    Assert.Equal("240 КС *K1/A1-K1/A2*", Apply(source, suggestion));
  }

  [Fact]
  public void CompletedPointRangeSuggestionFindsTokenInCommandBodyLine()
  {
    const string source = "240 КС\r\n  *K1/A1-2*";
    var model = new KsCommandModel { StartLineNumber = 1 };
    var warning = new WarningItem
    {
      SourceLineNumber = 1,
      Command = "240 КС",
      Code = WarningCode.Unknown,
      Description = "Запись диапазона (K1/A1-2) была дополнена. Убедитесь в правильности записи.",
    };
    var lines = new List<(int Offset, string Text)>
    {
      (0, "240 КС"),
      ("240 КС\r\n".Length, "  *K1/A1-2*"),
    };

    var suggestion = Assert.IsType<SourceTextSuggestion>(
      WarningSourceSuggestionFactory.Create(lines, model, warning, 2));

    Assert.Equal("240 КС\r\n  *K1/A1-K1/A2*", Apply(source, suggestion));
  }

  [Fact]
  public void EmptyMessageWarningHasNoInventedSuggestion()
  {
    const string source = "40 ЦУ";
    var model = new CuCommandModel { StartLineNumber = 1 };

    var suggestion = WarningSourceSuggestionFactory.Create(
      new List<(int Offset, string Text)> { (0, source) },
      model,
      GeneralWarnings.EmptyMessage(1, "40 ЦУ"));

    Assert.Null(suggestion);
  }

  [Fact]
  public void AmperageWarningUsesOwnCode()
  {
    var warning = GeneralWarnings.IgnoreAmperage(1, "180 ПР");

    Assert.Equal(WarningCode.Gen_IgnoreAmperage, warning.Code);
  }

  [Fact]
  public void SourceAnalysisAttachesReplacementToWarningDiagnostic()
  {
    const string source = "180 ПР 10В, Ом<5 *К1/11*";
    var manager = new CommandTranslationManager(new[] { new StubWarningParser() });

    var diagnostic = Assert.Single(
      manager.AnalyzeSource(source),
      item => item.Code == WarningCode.Gen_IgnoreVoltage.ToString());

    var suggestion = Assert.IsType<SourceTextSuggestion>(diagnostic.Suggestion);
    Assert.Equal("180 ПР Ом<5 *К1/11*", Apply(source, suggestion));
  }

  private static SourceTextSuggestion Create(string source, BaseCommandModel model, WarningItem warning)
  {
    var suggestion = WarningSourceSuggestionFactory.Create(
      new List<(int Offset, string Text)> { (0, source) },
      model,
      warning);
    return Assert.IsType<SourceTextSuggestion>(suggestion);
  }

  private static string Apply(string source, SourceTextSuggestion suggestion) =>
    source.Remove(suggestion.Offset, suggestion.ReplacementLength)
      .Insert(suggestion.Offset, suggestion.Text);

  private sealed class StubWarningParser : ICommandParser
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
      model.Warnings.Add(GeneralWarnings.IgnoreVoltage(numberLine, $"{commandNumber} {mnemonic}"));
      return model;
    }
  }
}
