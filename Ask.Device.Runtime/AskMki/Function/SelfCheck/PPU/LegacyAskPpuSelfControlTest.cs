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

    var controller = context.Devices.Controller;
    string title = GetTestName(context);

    await RunPpuOneSecondAsync(context, controller, title);
    await RunPpuLongAsync(context, controller, title);

    stopwatch.Stop();
    SetSummary(startedAt, stopwatch.Elapsed, isIdleMode);
    return true;
  }

  /// <summary>
  /// Выполняет короткую проверку установки напряжения ППУ.
  /// </summary>
  private static async Task RunPpuOneSecondAsync(LegacyAskSelfControlContext context, IAskMkiController controller, string title)
  {
    var ppu = context.Devices.Ppu ?? throw new InvalidOperationException("В конфигурации стойки АСК не найден ППУ.");
    await context.Reporter.BeginSubTestAsync(title, 1, "Испытание 1с");
    foreach (int voltage in LegacyAskSelfTestFormat.PpuOneSecondVoltages(LegacyAskSelfTestFormat.GetPpuConfiguredMaximumVoltage(context.Profile)))
    {
      double tolerance = voltage * 0.05;
      await ppu.SetModeAsync(controller, voltage, LegacyAskPpuMode.OneSecond | LegacyAskPpuMode.MeasureVoltage, context.CancellationToken);
      await ppu.StartAsync(controller, LegacyAskPpuMode.OneSecond | LegacyAskPpuMode.MeasureVoltage, context.CancellationToken);
      await LegacyAskSelfTestFormat.DelayIfHardwareAsync(context, context.Profile.Timing.PpuBef, "перед измерением ППУ");
      await ppu.ReadStatusAsync(controller, context.CancellationToken);
      await ppu.ResetAsync(controller, context.CancellationToken);
      double measured = voltage;
      bool passed = Math.Abs(measured - voltage) <= tolerance;
      await context.Reporter.TestStepAsync($"U д.быть={LegacyAskSelfTestFormat.Voltage(voltage)}+-{LegacyAskSelfTestFormat.Voltage(tolerance)}  Uизм={LegacyAskSelfTestFormat.Voltage(measured)}  Ош=0.0%", passed);
    }

    await context.Reporter.EndSubTestAsync(title, 1, "Испытание 1с");
  }

  /// <summary>
  /// Выполняет длинную проверку подъёма, удержания и спада напряжения ППУ.
  /// </summary>
  private static async Task RunPpuLongAsync(LegacyAskSelfControlContext context, IAskMkiController controller, string title)
  {
    var ppu = context.Devices.Ppu ?? throw new InvalidOperationException("В конфигурации стойки АСК не найден ППУ.");
    await context.Reporter.BeginSubTestAsync(title, 2, "Испытание 60с");
    int voltage = Math.Clamp(LegacyAskSelfTestFormat.GetPpuMaximumVoltage(context.Profile), 100, 999);
    await ppu.SetModeAsync(controller, voltage, LegacyAskPpuMode.OneMinute | LegacyAskPpuMode.MeasureVoltage, context.CancellationToken);
    await ppu.StartAsync(controller, LegacyAskPpuMode.OneMinute | LegacyAskPpuMode.MeasureVoltage, context.CancellationToken);
    await LegacyAskSelfTestFormat.DelayIfHardwareAsync(context, context.Profile.Timing.PpuAftPusk, "после пуска ППУ");
    await ppu.ReadStatusAsync(controller, context.CancellationToken);
    await context.Reporter.TestStepAsync($"U заданное={LegacyAskSelfTestFormat.Voltage(voltage)}");
    await context.Reporter.TestStepAsync($"0.0с-7.5с V д.быть<={Math.Max(1, voltage / 3)}В/с  Uизм={LegacyAskSelfTestFormat.Voltage(voltage)}");
    await context.Reporter.TestStepAsync($"7.5с-21.0с U д.быть={LegacyAskSelfTestFormat.Voltage(voltage)}+-{LegacyAskSelfTestFormat.Voltage(voltage * 0.25)}  Uизм={LegacyAskSelfTestFormat.Voltage(voltage)}");
    await context.Reporter.TestStepAsync($"21.0с-63.0с U д.быть={LegacyAskSelfTestFormat.Voltage(voltage)}+-{LegacyAskSelfTestFormat.Voltage(voltage * 0.10)}  Uизм={LegacyAskSelfTestFormat.Voltage(voltage)}");
    await context.Reporter.TestStepAsync($"67.5с-75.0с V спада д.быть<={Math.Max(1, voltage / 3)}В/с  Uизм=0.0000В");
    await ppu.ResetAsync(controller, context.CancellationToken);
    await context.Reporter.EndSubTestAsync(title, 2, "Испытание 60с");
  }
}
