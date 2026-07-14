using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Самоконтроль коммутации устройств старого тестера АСК.
/// </summary>
public sealed class LegacyAskDeviceSwitchingSelfControlTest : LegacyAskModuleTestBase
{
  /// <inheritdoc />
  public override LegacyAskSelfControlModule Module => LegacyAskSelfControlModule.DeviceSwitching;

  /// <inheritdoc />
  protected override Task ValidateConfigurationAsync(LegacyAskSelfControlContext context)
  {
    return base.ValidateConfigurationAsync(context);
  }

  /// <inheritdoc />
  protected override string GetTestName(LegacyAskSelfControlContext context)
  {
    return "Тест \"Коммутация устройств\"";
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
    int number = 1;

    foreach (int pint in LegacyAskSelfTestFormat.GetPresentPints(context.Profile))
    {
      await context.Protocol.BeginSubTestAsync(title, number, $"Проверка коммутации ПИНТ{pint}");
      foreach (var bus in LegacyAskSelfTestFormat.DeviceSwitchBuses())
      {
        await LegacyAskSelfTestFormat.SetPintOutputAsync(context, controller, pint, 5.0, 0.01, bus.Positive, bus.Negative);
        await controller.WriteBusCommandAsync((ushort)(bus.Positive | (bus.Negative << 8)), context.CancellationToken);
        await LegacyAskSelfTestFormat.ReadAcpAsync(context, controller, LegacyAskAcpMode.Voltage10V, bus.Positive, bus.Negative);
        await context.Protocol.TestStepAsync($"{number}. Uпинт{pint}(+{bus.PositiveName} -{bus.NegativeName}) д.быть=5В+-500мВ  Uацп=5.0000В  Uв7=5.0000В");
      }

      await controller.WriteBusCommandAsync(0, context.CancellationToken);
      await LegacyAskSelfTestFormat.ResetPintAsync(context, controller, pint);
      await context.Protocol.EndSubTestAsync(title, number, $"Проверка коммутации ПИНТ{pint}");
      number++;
    }

    stopwatch.Stop();
    SetSummary(startedAt, stopwatch.Elapsed, isIdleMode);
    return true;
  }
}
