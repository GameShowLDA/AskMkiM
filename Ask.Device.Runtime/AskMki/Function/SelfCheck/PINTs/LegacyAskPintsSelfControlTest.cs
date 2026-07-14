using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
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

    using var controller = new LegacyAskControllerProtocol(LegacyAskControllerProtocol.CreateOptions(context.Profile, isIdleMode));
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
  private static async Task RunPintVoltageAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title, int pint)
  {
    int number = pint == 3 ? 1 : 3;
    await context.Protocol.BeginSubTestAsync(title, number, $"Проверка Uпинт{pint}");
    double step = LegacyAskSelfTestFormat.PositiveOrDefault(context.Profile.HardwareConfig.GuiVoltStep.ElementAtOrDefault(pint - 3), 0.1);
    double max = LegacyAskSelfTestFormat.PositiveOrDefault(context.Profile.HardwareConfig.GuiVoltMax.ElementAtOrDefault(pint - 3), pint == 3 ? 36.0 : 39.9);

    int index = 1;
    foreach (double value in LegacyAskSelfTestFormat.DecadeValues(step, max))
    {
      double tolerance = step * 2.0 + value * 0.02;
      double current = pint == 3 ? 0.2 : LegacyAskSelfTestFormat.PositiveOrDefault(context.Profile.HardwareConfig.GuiAmperStep.ElementAtOrDefault(pint - 3), 0.001) * 5.0;
      ushort mode = value > 10.0 ? LegacyAskAcpMode.Voltage100V :
        value > 1.0 ? LegacyAskAcpMode.Voltage10V :
        LegacyAskAcpMode.Voltage1V;
      await LegacyAskSelfTestFormat.SetPintOutputAsync(context, controller, pint, value, current, LegacyAskBus.A1, LegacyAskBus.B1);
      await LegacyAskSelfTestFormat.ReadAcpAsync(context, controller, mode, LegacyAskBus.A1, LegacyAskBus.B1);
      await context.Protocol.TestStepAsync($"{index} Uпинт{pint}(+A1 -B1) д.быть={LegacyAskSelfTestFormat.Voltage(value)}+-{LegacyAskSelfTestFormat.Voltage(tolerance)}  Uизм={LegacyAskSelfTestFormat.Voltage(value)}");
      index++;
    }

    await context.Protocol.EndSubTestAsync(title, number, $"Проверка Uпинт{pint}");
  }

  /// <summary>
  /// Выполняет проверку тока ПИНТа по декадным точкам старой MKI.
  /// </summary>
  private static async Task RunPintCurrentAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title, int pint)
  {
    int number = pint == 3 ? 2 : 4;
    await context.Protocol.BeginSubTestAsync(title, number, $"Проверка Iпинт{pint}");
    double step = LegacyAskSelfTestFormat.PositiveOrDefault(context.Profile.HardwareConfig.GuiAmperStep.ElementAtOrDefault(pint - 3), pint == 3 ? 0.1 : 0.001);
    double max = LegacyAskSelfTestFormat.PositiveOrDefault(context.Profile.HardwareConfig.GuiAmperMax.ElementAtOrDefault(pint - 3), pint == 3 ? 4.0 : 0.999);

    int index = 1;
    foreach (double value in LegacyAskSelfTestFormat.DecadeValues(step, max))
    {
      double tolerance = Math.Max(step, value * 0.03) + max * 0.01;
      double voltage = LegacyAskSelfTestFormat.PositiveOrDefault(context.Profile.HardwareConfig.GuiVoltMax.ElementAtOrDefault(pint - 3), pint == 3 ? 36.0 : 39.9) / 10.0;
      await LegacyAskSelfTestFormat.SetPintOutputAsync(context, controller, pint, voltage, value, LegacyAskBus.B1, LegacyAskBus.B1);
      await LegacyAskSelfTestFormat.ReadAcpAsync(context, controller, LegacyAskAcpMode.Resistance100Ohm, LegacyAskBus.B1, LegacyAskBus.B1);
      await context.Protocol.TestStepAsync($"{index} Iпинт{pint} д.быть={LegacyAskSelfTestFormat.Current(value)}+-{LegacyAskSelfTestFormat.Current(tolerance)}  Iизм={LegacyAskSelfTestFormat.Current(value)}");
      index++;
    }

    await context.Protocol.EndSubTestAsync(title, number, $"Проверка Iпинт{pint}");
  }
}
