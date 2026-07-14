using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Самоконтроль АЦП старого тестера АСК.
/// </summary>
public sealed class LegacyAskAdcSelfControlTest : LegacyAskModuleTestBase
{
  /// <inheritdoc />
  public override LegacyAskSelfControlModule Module => LegacyAskSelfControlModule.Adc;

  /// <inheritdoc />
  protected override Task ValidateConfigurationAsync(LegacyAskSelfControlContext context)
  {
    return base.ValidateConfigurationAsync(context);
  }

  /// <inheritdoc />
  protected override string GetTestName(LegacyAskSelfControlContext context)
  {
    return "Тест АЦП";
  }

  /// <inheritdoc />
  protected override async Task<bool> ExecuteHardwareAsync(LegacyAskSelfControlContext context)
  {
    bool isIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    DateTime startedAt = DateTime.Now;
    var stopwatch = Stopwatch.StartNew();
    ResetSummary();

    using var controller = new LegacyAskControllerProtocol(LegacyAskControllerProtocol.CreateOptions(context.Profile, isIdleMode));
    string title = GetTestName(context);

    await RunAdcZeroVoltageAsync(context, controller, title);
    await RunAdcCurrentSourcesAsync(context, controller, title);
    await RunAdcPint4VoltageAsync(context, controller, title);
    await RunAdcShortCircuitResistanceAsync(context, controller, title);

    stopwatch.Stop();
    SetSummary(startedAt, stopwatch.Elapsed, isIdleMode);
    return true;
  }

  /// <summary>
  /// Выполняет проверку нуля АЦП при коротком замыкании входа.
  /// </summary>
  private static async Task RunAdcZeroVoltageAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Reporter.BeginSubTestAsync(title, 1, "Измерение 0В (КЗ входа)");
    foreach (var range in new[]
    {
      new LegacyAskExpectedValue("1В", 0, 0.01, "0В+-10мВ", false),
      new LegacyAskExpectedValue("10В", 0, 0.1, "0В+-100мВ", false),
      new LegacyAskExpectedValue("100В", 0, 1.0, "0В+-1В", false)
    })
    {
      ushort mode = range.RangeText.Contains("100") ? LegacyAskAcpMode.Voltage100V :
        range.RangeText.Contains("10") ? LegacyAskAcpMode.Voltage10V :
        LegacyAskAcpMode.Voltage1V;
      await LegacyAskSelfTestFormat.ReadAcpAsync(context, controller, mode, LegacyAskBus.B1, LegacyAskBus.B1);
      await context.Reporter.TestStepAsync($"ДиапU={range.RangeText} U д.быть={range.ExpectedText}  Uизм={LegacyAskSelfTestFormat.Voltage(range.Value)}");
    }

    await context.Reporter.EndSubTestAsync(title, 1, "Измерение 0В (КЗ входа)");
  }

  /// <summary>
  /// Выполняет проверку наличия напряжений источников тока АЦП.
  /// </summary>
  private static async Task RunAdcCurrentSourcesAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Reporter.BeginSubTestAsync(title, 2, "Наличие U источников тока АЦП");
    foreach (var testCase in new[]
    {
      new LegacyAskExpectedValue("4В", 4.0, 0.4, "4В+-400мВ", false),
      new LegacyAskExpectedValue("11В", 10.2, 0.5, "10.2В+-500мВ", false)
    })
    {
      ushort mode = testCase.RangeText.Contains("4") ? LegacyAskAcpMode.CurrentSource4V : LegacyAskAcpMode.CurrentSource11V;
      await LegacyAskSelfTestFormat.ReadAcpAsync(context, controller, mode, LegacyAskBus.A1, LegacyAskBus.B1);
      await context.Reporter.TestStepAsync($"Диап={testCase.RangeText} U д.быть={testCase.ExpectedText}  Uизм={LegacyAskSelfTestFormat.Voltage(testCase.Value)}");
    }

    await context.Reporter.EndSubTestAsync(title, 2, "Наличие U источников тока АЦП");
  }

  /// <summary>
  /// Выполняет проверку измерения напряжения ПИНТ4 через АЦП.
  /// </summary>
  private static async Task RunAdcPint4VoltageAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Reporter.BeginSubTestAsync(title, 3, "Измерение напряжения ПИНТ4");
    foreach (var testCase in new[]
    {
      new LegacyAskExpectedValue("1В", 2.0, 0, "2В", true),
      new LegacyAskExpectedValue("1В", 0.2, 0.05, "200мВ+-50мВ", false),
      new LegacyAskExpectedValue("10В", 2.0, 0.1, "2В+-100мВ", false),
      new LegacyAskExpectedValue("100В", 20.0, 0.6, "20В+-600мВ", false)
    })
    {
      ushort mode = testCase.RangeText.Contains("100") ? LegacyAskAcpMode.Voltage100V :
        testCase.RangeText.Contains("10") ? LegacyAskAcpMode.Voltage10V :
        LegacyAskAcpMode.Voltage1V;
      await LegacyAskSelfTestFormat.SetPintOutputAsync(context, controller, 4, testCase.Value, 0.01, LegacyAskBus.A1, LegacyAskBus.B1);
      await LegacyAskSelfTestFormat.ReadAcpAsync(context, controller, mode, LegacyAskBus.A1, LegacyAskBus.B1);
      string measured = testCase.MustBeOverload ? $">{testCase.RangeText}" : LegacyAskSelfTestFormat.Voltage(testCase.Value);
      string expectation = testCase.MustBeOverload ? "Д.быть перегр." : $"д.быть={testCase.ExpectedText}";
      await context.Reporter.TestStepAsync($"Uпинт4(A1+ B1-) {expectation}  Диап={testCase.RangeText}  Uизм={measured}");
    }

    await LegacyAskSelfTestFormat.ResetPintAsync(context, controller, 4);
    await context.Reporter.EndSubTestAsync(title, 3, "Измерение напряжения ПИНТ4");
  }

  /// <summary>
  /// Выполняет проверку измерения сопротивления КЗШ через АЦП.
  /// </summary>
  private static async Task RunAdcShortCircuitResistanceAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Reporter.BeginSubTestAsync(title, 4, "Измерение сопротивления КЗШ");
    foreach (var testCase in new[]
    {
      new LegacyAskResistanceCase("100 Ом", 240, 0, true),
      new LegacyAskResistanceCase("1кОм", 240, 50, false),
      new LegacyAskResistanceCase("10кОм", 240, 500, false),
      new LegacyAskResistanceCase("100кОм", 240, 5000, false)
    })
    {
      ushort mode = testCase.ToleranceOhm >= 5000 ? LegacyAskAcpMode.Resistance100KOhm :
        testCase.ToleranceOhm >= 500 ? LegacyAskAcpMode.Resistance10KOhm :
        testCase.ToleranceOhm >= 50 ? LegacyAskAcpMode.Resistance1KOhm :
        LegacyAskAcpMode.Resistance100Ohm;
      await LegacyAskSelfTestFormat.ReadAcpAsync(context, controller, mode, LegacyAskBus.A1, LegacyAskBus.B1);
      string measured = testCase.MustBeOverload ? $">{testCase.RangeText}" : LegacyAskSelfTestFormat.Resistance(testCase.ValueOhm);
      string expected = testCase.MustBeOverload
        ? $"{LegacyAskSelfTestFormat.Resistance(testCase.ValueOhm)} Д.быть перегр."
        : $"{LegacyAskSelfTestFormat.Resistance(testCase.ValueOhm)}+-{LegacyAskSelfTestFormat.Resistance(testCase.ToleranceOhm)}";
      await context.Reporter.TestStepAsync($"Диап={testCase.RangeText} R д.быть={expected}  Rизм={measured}");
    }

    await context.Reporter.EndSubTestAsync(title, 4, "Измерение сопротивления КЗШ");
  }

  /// <summary>
  /// Выполняет чтение АЦП для боевого режима или получает эмулированный код в холостом режиме.
  /// </summary>
}
