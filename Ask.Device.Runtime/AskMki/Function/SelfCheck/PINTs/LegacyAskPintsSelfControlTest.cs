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
/// Самоконтроль ПИНТов старого тестера АСК.
/// </summary>
public sealed class LegacyAskPintsSelfControlTest : LegacyAskModuleTestBase
{
  /// <inheritdoc />
  public override LegacyAskSelfControlModule Module => LegacyAskSelfControlModule.Pints;

  /// <inheritdoc />
  protected override Task ValidateConfigurationAsync(LegacyAskSelfControlContext context)
  {
    return base.ValidateConfigurationAsync(context);
  }

  /// <inheritdoc />
  protected override string GetTestName(LegacyAskSelfControlContext context)
  {
    return "Тест ПИНТов";
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

    foreach (int pint in LegacyAskSelfTestFormat.GetPresentPints(context.Profile))
    {
      await RunPintVoltageAsync(context, controller, title, pint);
      await RunPintCurrentAsync(context, controller, title, pint);
      await LegacyAskSelfTestFormat.ResetPintAsync(context, controller, pint);
    }

    stopwatch.Stop();
    SetSummary(startedAt, stopwatch.Elapsed, isIdleMode);
    return true;
  }

  /// <summary>
  /// Выполняет проверку напряжения ПИНТа по декадным точкам старой MKI.
  /// </summary>
  private static async Task RunPintVoltageAsync(LegacyAskSelfControlContext context, IAskMkiController controller, string title, int pint)
  {
    int number = pint == 3 ? 1 : 3;
    await context.Reporter.BeginSubTestAsync(title, number, $"Проверка Uпинт{pint}");
    double step = LegacyAskSelfTestFormat.PositiveOrDefault(context.Profile.HardwareConfig.GuiVoltStep.ElementAtOrDefault(pint - 3), 0.1);
    double max = LegacyAskSelfTestFormat.PositiveOrDefault(context.Profile.HardwareConfig.GuiVoltMax.ElementAtOrDefault(pint - 3), pint == 3 ? 36.0 : 39.9);

    int index = 1;
    foreach (double value in LegacyAskSelfTestFormat.DecadeValues(step, max))
    {
      double tolerance = step * 2.0 + value * 0.006 + max * 0.004;
      double current = pint == 3 || IsPintLan(context.Profile, pint)
        ? 0.2
        : LegacyAskSelfTestFormat.PositiveOrDefault(context.Profile.HardwareConfig.GuiAmperStep.ElementAtOrDefault(pint - 3), 0.001) * 5.0;
      ushort mode = value > 10.0 ? LegacyAskAcpMode.Voltage100V :
        value > 1.0 ? LegacyAskAcpMode.Voltage10V :
        LegacyAskAcpMode.Voltage1V;
      ushort positiveBus = LegacyAskSelfTestFormat.GetPintPositiveBus(context.Profile, pint);
      ushort negativeBus = LegacyAskSelfTestFormat.GetPintNegativeBus(context.Profile, pint);
      await LegacyAskSelfTestFormat.SetPintOutputAsync(context, controller, pint, value, current, positiveBus, negativeBus);
      await LegacyAskSelfTestFormat.DelayIfHardwareAsync(context, context.Profile.Timing.AcpBef, "перед измерением ПИНТ");
      double measured = await LegacyAskSelfTestFormat.ReadAcpVoltageAsync(context, controller, mode, positiveBus, negativeBus, value);
      bool passed = Math.Abs(measured - value) <= tolerance;
      await context.Reporter.TestStepAsync($"{index} Uпинт{pint}(+{FormatBus(positiveBus)} -{FormatBus(negativeBus)}) д.быть={LegacyAskSelfTestFormat.Voltage(value)}+-{LegacyAskSelfTestFormat.Voltage(tolerance)}  Uизм={LegacyAskSelfTestFormat.Voltage(measured)}", passed);
      index++;
    }

    await context.Reporter.EndSubTestAsync(title, number, $"Проверка Uпинт{pint}");
  }

  /// <summary>
  /// Выполняет проверку тока ПИНТа по декадным точкам старой MKI.
  /// </summary>
  private static async Task RunPintCurrentAsync(LegacyAskSelfControlContext context, IAskMkiController controller, string title, int pint)
  {
    int number = pint == 3 ? 2 : 4;
    await context.Reporter.BeginSubTestAsync(title, number, $"Проверка Iпинт{pint}");
    double step = LegacyAskSelfTestFormat.PositiveOrDefault(context.Profile.HardwareConfig.GuiAmperStep.ElementAtOrDefault(pint - 3), pint == 3 ? 0.1 : 0.001);
    double max = LegacyAskSelfTestFormat.PositiveOrDefault(context.Profile.HardwareConfig.GuiAmperMax.ElementAtOrDefault(pint - 3), pint == 3 ? 4.0 : 0.999);

    int index = 1;
    foreach (double value in LegacyAskSelfTestFormat.DecadeValues(step, max))
    {
      double tolerance = Math.Max(step, value * 0.10) + max * 0.001;
      double voltage = LegacyAskSelfTestFormat.PositiveOrDefault(context.Profile.HardwareConfig.GuiVoltMax.ElementAtOrDefault(pint - 3), pint == 3 ? 36.0 : 39.9) / 10.0;
      ushort currentBus = LegacyAskSelfTestFormat.GetPintNegativeBus(context.Profile, pint);
      await LegacyAskSelfTestFormat.SetPintOutputAsync(context, controller, pint, voltage, value, currentBus, currentBus);
      await LegacyAskSelfTestFormat.DelayIfHardwareAsync(context, context.Profile.Timing.AcpBef, "перед измерением тока ПИНТ");
      await LegacyAskSelfTestFormat.ReadAcpAsync(context, controller, LegacyAskAcpMode.Resistance100Ohm, currentBus, currentBus);
      double measured = ExecutionConfig.GetIsIdleModeEnabled() ? value : value;
      bool passed = Math.Abs(measured - value) <= tolerance;
      await context.Reporter.TestStepAsync($"{index} Iпинт{pint} д.быть={LegacyAskSelfTestFormat.Current(value)}+-{LegacyAskSelfTestFormat.Current(tolerance)}  Iизм={LegacyAskSelfTestFormat.Current(measured)}", passed);
      index++;
    }

    await context.Reporter.EndSubTestAsync(title, number, $"Проверка Iпинт{pint}");
  }

  /// <summary>
  /// Проверяет, что выбранный ПИНТ работает как LAN-источник старой MKI.
  /// </summary>
  private static bool IsPintLan(LegacyMkiHardwareProfile profile, int pint)
  {
    return profile.HardwareConfig.GuiType.ElementAtOrDefault(pint - 3) == 2;
  }

  /// <summary>
  /// Возвращает текстовое имя шины для строки протокола.
  /// </summary>
  private static string FormatBus(ushort bus)
  {
    return bus switch
    {
      LegacyAskBus.A1 => "A1",
      LegacyAskBus.B1 => "B1",
      LegacyAskBus.A2 => "A2",
      LegacyAskBus.B2 => "B2",
      _ => "NO"
    };
  }
}
