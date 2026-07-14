using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
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

    using var controller = new LegacyAskControllerProtocol(LegacyAskControllerProtocol.CreateOptions(context.Profile, isIdleMode));
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
  private static async Task RunCommutatorNoShortsAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Protocol.BeginSubTestAsync(title, 1, "Проверка отсутствия лишних замыканий");
    foreach (var range in LegacyAskSelfTestFormat.GetSwitchRanges(context.Profile))
    {
      foreach (ushort address in LegacyAskSelfTestFormat.GetProbeAddresses(range))
      {
        await controller.CheckNoElectronicConnectionAsync(address, context.CancellationToken);
      }
      await context.Protocol.TestStepAsync($"{range.Name} БК {range.FirstBk}-{range.LastBk}: лишние соединения не обнаружены");
    }

    await context.Protocol.EndSubTestAsync(title, 1, "Проверка отсутствия лишних замыканий");
  }

  /// <summary>
  /// Выполняет проверку отсутствия обрывов коммутатора.
  /// </summary>
  private static async Task RunCommutatorNoBreaksAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Protocol.BeginSubTestAsync(title, 2, "Проверка отсутствия обрывов");
    foreach (var range in LegacyAskSelfTestFormat.GetSwitchRanges(context.Profile))
    {
      foreach (ushort address in LegacyAskSelfTestFormat.GetProbeAddresses(range))
      {
        await controller.CheckElectronicConnectionAsync(address, context.CancellationToken);
        await controller.CheckElectronicDisconnectionAsync(address, context.CancellationToken);
      }
      await context.Protocol.TestStepAsync($"{range.Name} БК {range.FirstBk}-{range.LastBk}: цепи подключаются нормально");
    }

    await context.Protocol.EndSubTestAsync(title, 2, "Проверка отсутствия обрывов");
  }

  /// <summary>
  /// Выполняет проверку сопротивления контактов реле коммутатора.
  /// </summary>
  private static async Task RunCommutatorContactResistanceAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Protocol.BeginSubTestAsync(title, 3, "Сопротивление контактов реле");
    foreach (var range in LegacyAskSelfTestFormat.GetSwitchRanges(context.Profile))
    {
      await controller.WriteCommandRegisterAsync((ushort)(LegacyAskCommandBits.RelayA | LegacyAskCommandBits.RelayB | LegacyAskCommandBits.GroupRelay), context.CancellationToken);
      await controller.ReadAdcAsync(context.CancellationToken);
      await context.Protocol.TestStepAsync($"{range.Name} БК {range.FirstBk}-{range.LastBk}: Rконт={LegacyAskSelfTestFormat.Resistance(context.Profile.HardwareConfig.RbusBb)} [НОРМА]");
    }

    await context.Protocol.EndSubTestAsync(title, 3, "Сопротивление контактов реле");
  }

  /// <summary>
  /// Выполняет проверку сопротивления изоляции коммутатора.
  /// </summary>
  private static async Task RunCommutatorInsulationAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, string title)
  {
    await context.Protocol.BeginSubTestAsync(title, 4, "Сопротивление изоляции коммутатора");
    foreach (var range in LegacyAskSelfTestFormat.GetSwitchRanges(context.Profile))
    {
      await controller.WriteCommandRegisterAsync((ushort)(LegacyAskCommandBits.ElectronicProbe | LegacyAskCommandBits.ElectronicTop | LegacyAskCommandBits.ElectronicBottom), context.CancellationToken);
      await controller.ReadAdcAsync(context.CancellationToken);
      await context.Protocol.TestStepAsync($"{range.Name} БК {range.FirstBk}-{range.LastBk}: Rиз>{context.Profile.HardwareConfig.GomCmt:0.###} ГОм [НОРМА]");
    }

    await context.Protocol.EndSubTestAsync(title, 4, "Сопротивление изоляции коммутатора");
  }
}
