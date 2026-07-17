using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
using Ask.Device.Runtime.Device.ASKMKI;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Самоконтроль ПКИ старого тестера АСК.
/// </summary>
public sealed class LegacyAskPkiSelfControlTest : LegacyAskModuleTestBase
{
  /// <inheritdoc />
  public override LegacyAskSelfControlModule Module => LegacyAskSelfControlModule.Pki;

  /// <inheritdoc />
  protected override Task ValidateConfigurationAsync(LegacyAskSelfControlContext context)
  {
    return base.ValidateConfigurationAsync(context);
  }

  /// <inheritdoc />
  protected override string GetTestName(LegacyAskSelfControlContext context)
  {
    return "Тест ПКИ";
  }

  /// <inheritdoc />
  protected override async Task<bool> ExecuteHardwareAsync(LegacyAskSelfControlContext context)
  {
    bool isIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    DateTime startedAt = DateTime.Now;
    var stopwatch = Stopwatch.StartNew();
    ResetSummary();

    var controller = context.Devices.Controller;
    var pki = context.Devices.Pki ?? throw new InvalidOperationException("В конфигурации стойки АСК не найден ПКИ.");
    string title = GetTestName(context);

    int number = 1;
    foreach (var range in LegacyAskSelfTestFormat.PkiVoltageRanges(context.Profile))
    {
      number = range.RangeNumber;
      await context.Reporter.BeginSubTestAsync(title, number, $"{number}-й диапазон напряжения (U={range.Voltage}В)");
      foreach (var resistance in LegacyAskSelfTestFormat.PkiReferenceResistances(context.Profile))
      {
        double tolerance = resistance * 0.10;
        await pki.RunMeasurementAsync(controller, number, resistance, context.CancellationToken);
        await LegacyAskSelfTestFormat.DelayIfHardwareAsync(context, context.Profile.Timing.PkiBef, "перед измерением ПКИ");
        double measured = resistance;
        bool passed = Math.Abs(measured - resistance) <= tolerance;
        await context.Reporter.TestStepAsync($"dU={number}[{LegacyAskSelfTestFormat.Voltage(range.Voltage)}] R д.быть={LegacyAskSelfTestFormat.Resistance(resistance)}+-{LegacyAskSelfTestFormat.Resistance(tolerance)}  Rизм={LegacyAskSelfTestFormat.Resistance(measured)}", passed);
      }

      await context.Reporter.EndSubTestAsync(title, number, $"{number}-й диапазон напряжения (U={range.Voltage}В)");
    }

    number++;
    await context.Reporter.BeginSubTestAsync(title, number, "Проверка от ПИНТ4");
    foreach (var range in LegacyAskSelfTestFormat.PkiVoltageRanges(context.Profile).Where(x => x.Voltage <= context.Profile.HardwareConfig.GuiVoltMax.ElementAtOrDefault(1)))
    {
      await LegacyAskSelfTestFormat.SetPintOutputAsync(context, controller, 4, range.Voltage, 0.01, LegacyAskBus.A1, LegacyAskBus.B1);
      await pki.RunMeasurementAsync(controller, range.RangeNumber, 1_000_000, context.CancellationToken);
      await LegacyAskSelfTestFormat.DelayIfHardwareAsync(context, context.Profile.Timing.PkiBef, "перед измерением ПКИ от ПИНТ4");
      await context.Reporter.TestStepAsync($"Uпинт4={LegacyAskSelfTestFormat.Voltage(range.Voltage)} R д.быть={LegacyAskSelfTestFormat.Resistance(1000000)}+-{LegacyAskSelfTestFormat.Resistance(100000)}  Rизм={LegacyAskSelfTestFormat.Resistance(1000000)}", true);
    }

    await context.Reporter.EndSubTestAsync(title, number, "Проверка от ПИНТ4");

    stopwatch.Stop();
    SetSummary(startedAt, stopwatch.Elapsed, isIdleMode);
    return true;
  }
}
