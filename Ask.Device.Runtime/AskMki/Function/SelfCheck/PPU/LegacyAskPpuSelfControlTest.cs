using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Самоконтроль ППУ старого тестера АСК.
/// </summary>
public sealed class LegacyAskPpuSelfControlTest : LegacyAskModuleTestBase
{
  /// <inheritdoc />
  public override LegacyAskSelfControlModule Module => LegacyAskSelfControlModule.Ppu;

  /// <inheritdoc />
  protected override Task ValidateConfigurationAsync(LegacyAskSelfControlContext context)
  {
    return base.ValidateConfigurationAsync(context);
  }

  /// <inheritdoc />
  protected override string GetTestName(LegacyAskSelfControlContext context)
  {
    return "Тест ППУ";
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
    var ppuController = ppuPkiController ?? controller;
    string title = GetTestName(context);

    await RunPpuOneSecondAsync(context, ppuController, title);
    await RunPpuLongAsync(context, ppuController, title);

    stopwatch.Stop();
    SetSummary(startedAt, stopwatch.Elapsed, isIdleMode);
    return true;
  }

  /// <summary>
  /// Выполняет короткую проверку установки напряжения ППУ.
  /// </summary>
  private static async Task RunPpuOneSecondAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Reporter.BeginSubTestAsync(title, 1, "Испытание 1с");
    foreach (int voltage in LegacyAskSelfTestFormat.PpuOneSecondVoltages(LegacyAskSelfTestFormat.GetPpuMaximumVoltage(context.Profile)))
    {
      double tolerance = voltage * 0.05;
      await LegacyAskPpuPkiExchange.SetPpuModeAsync(context, controller, voltage, LegacyAskPpuMode.OneSecond | LegacyAskPpuMode.MeasureVoltage);
      await LegacyAskPpuPkiExchange.StartPpuAsync(context, controller, LegacyAskPpuMode.OneSecond | LegacyAskPpuMode.MeasureVoltage);
      await LegacyAskPpuPkiExchange.ReadPpuStatusAsync(context, controller);
      await LegacyAskPpuPkiExchange.ResetPpuAsync(context, controller);
      await context.Reporter.TestStepAsync($"U д.быть={LegacyAskSelfTestFormat.Voltage(voltage)}+-{LegacyAskSelfTestFormat.Voltage(tolerance)}  Uизм={LegacyAskSelfTestFormat.Voltage(voltage)}  Ош=0.0%");
    }

    await context.Reporter.EndSubTestAsync(title, 1, "Испытание 1с");
  }

  /// <summary>
  /// Выполняет длинную проверку подъёма, удержания и спада напряжения ППУ.
  /// </summary>
  private static async Task RunPpuLongAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Reporter.BeginSubTestAsync(title, 2, "Испытание 60с");
    int voltage = Math.Clamp(LegacyAskSelfTestFormat.GetPpuMaximumVoltage(context.Profile), 100, 999);
    await LegacyAskPpuPkiExchange.SetPpuModeAsync(context, controller, voltage, LegacyAskPpuMode.OneMinute | LegacyAskPpuMode.MeasureVoltage);
    await LegacyAskPpuPkiExchange.StartPpuAsync(context, controller, LegacyAskPpuMode.OneMinute | LegacyAskPpuMode.MeasureVoltage);
    await LegacyAskPpuPkiExchange.ReadPpuStatusAsync(context, controller);
    await context.Reporter.TestStepAsync($"U заданное={LegacyAskSelfTestFormat.Voltage(voltage)}");
    await context.Reporter.TestStepAsync($"0.0с-7.5с V д.быть<={Math.Max(1, voltage / 3)}В/с  Uизм={LegacyAskSelfTestFormat.Voltage(voltage)}");
    await context.Reporter.TestStepAsync($"7.5с-21.0с U д.быть={LegacyAskSelfTestFormat.Voltage(voltage)}+-{LegacyAskSelfTestFormat.Voltage(voltage * 0.25)}  Uизм={LegacyAskSelfTestFormat.Voltage(voltage)}");
    await context.Reporter.TestStepAsync($"21.0с-63.0с U д.быть={LegacyAskSelfTestFormat.Voltage(voltage)}+-{LegacyAskSelfTestFormat.Voltage(voltage * 0.05)}  Uизм={LegacyAskSelfTestFormat.Voltage(voltage)}");
    await context.Reporter.TestStepAsync($"67.5с-75.0с V спада д.быть<={Math.Max(1, voltage / 3)}В/с  Uизм=0.0000В");
    await LegacyAskPpuPkiExchange.ResetPpuAsync(context, controller);
    await context.Reporter.EndSubTestAsync(title, 2, "Испытание 60с");
  }
}
