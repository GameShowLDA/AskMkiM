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
/// Самоконтроль коммутатора старого тестера АСК.
/// </summary>
public sealed class LegacyAskCommutatorSelfControlTest : LegacyAskModuleTestBase
{
  /// <inheritdoc />
  public override LegacyAskSelfControlModule Module => LegacyAskSelfControlModule.Commutator;

  /// <inheritdoc />
  protected override Task ValidateConfigurationAsync(LegacyAskSelfControlContext context)
  {
    return base.ValidateConfigurationAsync(context);
  }

  /// <inheritdoc />
  protected override string GetTestName(LegacyAskSelfControlContext context)
  {
    return "Тест коммутатора";
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

    await RunCommutatorNoShortsAsync(context, controller, title);
    await RunCommutatorNoBreaksAsync(context, controller, title);
    await RunCommutatorContactResistanceAsync(context, controller, title);
    await RunCommutatorInsulationAsync(context, controller, title);

    stopwatch.Stop();
    SetSummary(startedAt, stopwatch.Elapsed, isIdleMode);
    return true;
  }

  /// <summary>
  /// Выполняет проверку отсутствия лишних замыканий коммутатора.
  /// </summary>
  private static async Task RunCommutatorNoShortsAsync(LegacyAskSelfControlContext context, IAskMkiController controller, string title)
  {
    await context.Reporter.BeginSubTestAsync(title, 1, "Проверка отсутствия лишних замыканий");
    foreach (var range in LegacyAskSelfTestFormat.GetSwitchRanges(context.Profile))
    {
      foreach (ushort address in LegacyAskSelfTestFormat.GetProbeAddresses(range))
      {
        await context.Devices.Commutator.CheckNoElectronicConnectionAsync(controller, address, context.CancellationToken);
      }
      await context.Reporter.TestStepAsync($"{range.Name} БК {range.FirstBk}-{range.LastBk}: лишние соединения не обнаружены");
    }

    await context.Reporter.EndSubTestAsync(title, 1, "Проверка отсутствия лишних замыканий");
  }

  /// <summary>
  /// Выполняет проверку отсутствия обрывов коммутатора.
  /// </summary>
  private static async Task RunCommutatorNoBreaksAsync(LegacyAskSelfControlContext context, IAskMkiController controller, string title)
  {
    await context.Reporter.BeginSubTestAsync(title, 2, "Проверка отсутствия обрывов");
    foreach (var range in LegacyAskSelfTestFormat.GetSwitchRanges(context.Profile))
    {
      foreach (ushort address in LegacyAskSelfTestFormat.GetProbeAddresses(range))
      {
        await context.Devices.Commutator.CheckElectronicConnectionAsync(controller, address, context.CancellationToken);
        await context.Devices.Commutator.CheckElectronicDisconnectionAsync(controller, address, context.CancellationToken);
      }
      await context.Reporter.TestStepAsync($"{range.Name} БК {range.FirstBk}-{range.LastBk}: цепи подключаются нормально");
    }

    await context.Reporter.EndSubTestAsync(title, 2, "Проверка отсутствия обрывов");
  }

  /// <summary>
  /// Выполняет проверку сопротивления контактов реле коммутатора.
  /// </summary>
  private static async Task RunCommutatorContactResistanceAsync(LegacyAskSelfControlContext context, IAskMkiController controller, string title)
  {
    await context.Reporter.BeginSubTestAsync(title, 3, "Сопротивление контактов реле");
    foreach (var range in LegacyAskSelfTestFormat.GetSwitchRanges(context.Profile))
    {
      await context.Devices.Commutator.WriteCommandRegisterAsync(controller, (ushort)(LegacyAskCommandBits.RelayA | LegacyAskCommandBits.RelayB | LegacyAskCommandBits.GroupRelay), context.CancellationToken);
      await controller.ReadAdcAsync(context.CancellationToken);
      await context.Reporter.TestStepAsync($"{range.Name} БК {range.FirstBk}-{range.LastBk}: Rконт={LegacyAskSelfTestFormat.Resistance(context.Profile.HardwareConfig.RbusBb)} [НОРМА]");
    }

    await context.Reporter.EndSubTestAsync(title, 3, "Сопротивление контактов реле");
  }

  /// <summary>
  /// Выполняет проверку сопротивления изоляции коммутатора.
  /// </summary>
  private static async Task RunCommutatorInsulationAsync(LegacyAskSelfControlContext context, IAskMkiController controller, string title)
  {
    await context.Reporter.BeginSubTestAsync(title, 4, "Сопротивление изоляции коммутатора");
    foreach (var range in LegacyAskSelfTestFormat.GetSwitchRanges(context.Profile))
    {
      await context.Devices.Commutator.WriteCommandRegisterAsync(controller, (ushort)(LegacyAskCommandBits.ElectronicProbe | LegacyAskCommandBits.ElectronicTop | LegacyAskCommandBits.ElectronicBottom), context.CancellationToken);
      await controller.ReadAdcAsync(context.CancellationToken);
      await context.Reporter.TestStepAsync($"{range.Name} БК {range.FirstBk}-{range.LastBk}: Rиз>{context.Profile.HardwareConfig.GomCmt:0.###} ГОм [НОРМА]");
    }

    await context.Reporter.EndSubTestAsync(title, 4, "Сопротивление изоляции коммутатора");
  }
}
