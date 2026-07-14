using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
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

    using var controller = new LegacyAskControllerProtocol(LegacyAskControllerProtocol.CreateOptions(context.Profile, isIdleMode));
    using var ppuPkiController = LegacyAskPpuPkiExchange.CreatePpuPkiController(context.Profile, isIdleMode);
    var pkiController = ppuPkiController ?? controller;
    string title = GetTestName(context);

    int number = 1;
    foreach (int voltage in new[] { 6, 10, 30 }.Where(x => x <= context.Profile.HardwareConfig.PkiUmax))
    {
      await context.Reporter.BeginSubTestAsync(title, number, $"{number}-й диапазон напряжения (U={voltage}В)");
      foreach (var resistance in LegacyAskSelfTestFormat.PkiReferenceResistances(context.Profile).Take(8))
      {
        double tolerance = resistance * 0.10;
        await LegacyAskPpuPkiExchange.RunPkiMeasurementAsync(context, pkiController, number, resistance);
        await context.Reporter.TestStepAsync($"dU={number}[{LegacyAskSelfTestFormat.Voltage(voltage)}] R д.быть={LegacyAskSelfTestFormat.Resistance(resistance)}+-{LegacyAskSelfTestFormat.Resistance(tolerance)}  Rизм={LegacyAskSelfTestFormat.Resistance(resistance)}");
      }

      await context.Reporter.EndSubTestAsync(title, number, $"{number}-й диапазон напряжения (U={voltage}В)");
      number++;
    }

    await context.Reporter.BeginSubTestAsync(title, number, "Проверка от ПИНТ4");
    foreach (int voltage in new[] { 6, 10, 30 }.Where(x => x <= context.Profile.HardwareConfig.PkiUmax && x <= context.Profile.HardwareConfig.GuiVoltMax.ElementAtOrDefault(1)))
    {
      await LegacyAskSelfTestFormat.SetPintOutputAsync(context, controller, 4, voltage, 0.01, LegacyAskBus.A1, LegacyAskBus.B1);
      await LegacyAskPpuPkiExchange.RunPkiMeasurementAsync(context, pkiController, Math.Max(1, number - 1), 1_000_000);
      await context.Reporter.TestStepAsync($"Uпинт4={LegacyAskSelfTestFormat.Voltage(voltage)} R д.быть={LegacyAskSelfTestFormat.Resistance(1000000)}+-{LegacyAskSelfTestFormat.Resistance(100000)}  Rизм={LegacyAskSelfTestFormat.Resistance(1000000)}");
    }

    await context.Reporter.EndSubTestAsync(title, number, "Проверка от ПИНТ4");

    stopwatch.Stop();
    SetSummary(startedAt, stopwatch.Elapsed, isIdleMode);
    return true;
  }
}
