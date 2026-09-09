using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Device.Emulator.Chassis;
using Ask.Device.Emulator.BreakdownTester;
using Ask.Device.Emulator.DeviceBusCommutation;
using Ask.Device.Emulator.ModuleRelayControl;
using Ask.Device.Emulator.Multimeter;
using Ask.Device.Emulator.Protocols;
using System.Runtime.CompilerServices;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Emulator
{
  /// <summary>
  /// Создаёт протоколы устройств с поддержкой эмуляции в холостом режиме.
  /// </summary>
  public static class DeviceProtocolEmulator
  {
    private static readonly ConditionalWeakTable<IChassisManager, ChassisProtocolEntry> Chassis = new();
    private static readonly ConditionalWeakTable<IRelaySwitchModule, RelayProtocolEntry> RelaySwitchModules = new();
    private static readonly ConditionalWeakTable<ISwitchingDevice, IDeviceProtocol> SwitchingDevices = new();

    /// <summary>
    /// Создаёт протокол ППУ с единым логированием и автоматическим выбором Real/Idle.
    /// </summary>
    public static IDeviceProtocol CreateBreakdownTester(
      IBreakdownTester device,
      IDeviceProtocol realProtocol)
    {
      ArgumentNullException.ThrowIfNull(device);
      ArgumentNullException.ThrowIfNull(realProtocol);
      return new BreakdownTesterCommandProtocol(device, realProtocol);
    }

    /// <summary>
    /// Выполняет команду мультиметра через реальный протокол или эмулятор и записывает обмен в журнал.
    /// </summary>
    public static async Task<string> QueryMultimeterAsync(
      IMultimeter device,
      string command,
      string idleResponse,
      double responseDelay = 0,
      int timeout = 0,
      int port = 0,
      CancellationToken cancellationToken = default)
    {
      ArgumentNullException.ThrowIfNull(device);

      string mode = ExecutionConfig.GetIsIdleModeEnabled() ? "Холостой режим" : "Реальное обращение";
      string name = $"{device.Name}({device.NumberChassis}.{device.Number})";
      LogInformation($"{mode} | [{name}] Команда мультиметра: \"{command}\".", isDeviceLog: true);

      var protocol = new ModeSelectingDeviceProtocol(
        () => device.DeviceProtocol,
        new MultimeterEmulatorProtocol(
          idleResponse,
          () => IdleHardwareErrorSimulator.ShouldSimulateHardwareError(device)));
      bool expectsResponse = command.Contains('?');
      string response = await protocol.QueryAsync(
        command,
        expectsResponse ? responseDelay : 0,
        expectsResponse ? timeout : 0,
        port,
        cancellationToken: cancellationToken);

      LogInformation(
        $"{mode} | [{name}] Ответ мультиметра на \"{command}\": \"{(string.IsNullOrEmpty(response) ? "<пустой>" : response)}\".",
        isDeviceLog: true);
      return response;
    }

    /// <summary>
    /// Создаёт общий протокол эмуляции для экземпляра УКШ.
    /// </summary>
    public static IDeviceProtocol CreateDeviceBusCommutation(ISwitchingDevice device)
    {
      ArgumentNullException.ThrowIfNull(device);
      return SwitchingDevices.GetValue(
        device,
        item => new ModeSelectingDeviceProtocol(
          () => item.DeviceProtocol,
          new DeviceBusCommutationEmulatorProtocol(
            () => item.Number,
            () => item.NumberChassis,
            () => IdleHardwareErrorSimulator.ShouldSimulateHardwareError(item),
            ExecutionConfig.GetIsErrorSimulationEnabled,
            () => Random.Shared.Next(4) == 0 ? Random.Shared.Next(1, 256) : 0)));
    }

    /// <summary>
    /// Создаёт общий stateful-протокол эмуляции для экземпляра МКР.
    /// </summary>
    public static IDeviceProtocol CreateModuleRelayControl(IRelaySwitchModule module)
    {
      ArgumentNullException.ThrowIfNull(module);
      return CreateModuleRelayControl(
        module,
        () => module.DeviceProtocol,
        () => module.Number,
        () => module.NumberChassis);
    }

    /// <summary>
    /// Создаёт протокол модуля коммутации реле с автоматическим выбором режима.
    /// </summary>
    public static IDeviceProtocol CreateModuleRelayControl(
      IRelaySwitchModule module,
      Func<IDeviceProtocol?> realProtocolProvider,
      Func<int> moduleNumberProvider,
      Func<int> chassisNumberProvider)
    {
      ArgumentNullException.ThrowIfNull(module);

      return RelaySwitchModules.GetValue(module, device =>
      {
        var emulator = new ModuleRelayControlEmulatorProtocol(
          moduleNumberProvider,
          chassisNumberProvider,
          () => IdleHardwareErrorSimulator.ShouldSimulateHardwareError(device),
          ExecutionConfig.GetIsErrorSimulationEnabled,
          () => Random.Shared.Next(3),
          () => EquipmentExecutionContext.IsSelfTest || IsRelayPowerAvailable(chassisNumberProvider()));
        return new RelayProtocolEntry(
          new ModeSelectingDeviceProtocol(realProtocolProvider, emulator), emulator);
      }).Protocol;
    }

    /// <summary>
    /// Связывает общий сброс эмулятора с состоянием точек и шин runtime-МКР.
    /// </summary>
    public static void RegisterRelayChassisReset(IRelaySwitchModule module, Action onReset)
    {
      ArgumentNullException.ThrowIfNull(onReset);
      CreateModuleRelayControl(module);
      if (RelaySwitchModules.TryGetValue(module, out var entry))
      {
        entry.Emulator.ChassisReset += onReset;
      }
    }

    private static bool IsRelayPowerAvailable(int chassisNumber)
    {
      return !Chassis.Any(entry => entry.Key.Number == chassisNumber && !entry.Value.Emulator.PowerEnabled);
    }

    private static void ResetRelayModules(int chassisNumber)
    {
      foreach (var entry in RelaySwitchModules)
      {
        if (entry.Key.NumberChassis == chassisNumber && !EquipmentExecutionContext.IsSelfTest)
        {
          entry.Value.Emulator.ResetFromChassis();
        }
      }
    }

    /// <summary>
    /// Создаёт протокол контроллера шасси с автоматическим выбором режима.
    /// </summary>
    public static IDeviceProtocol CreateChassis(IChassisManager chassis)
    {
      ArgumentNullException.ThrowIfNull(chassis);
      return Chassis.GetValue(
        chassis,
        device =>
        {
          var emulator = new ChassisEmulatorProtocol(
            () => IdleHardwareErrorSimulator.ShouldSimulateHardwareError(device),
            () => ResetRelayModules(device.Number));
          return new ChassisProtocolEntry(
            new ModeSelectingDeviceProtocol(() => device.DeviceProtocol, emulator), emulator);
        }).Protocol;
    }

    private sealed record ChassisProtocolEntry(IDeviceProtocol Protocol, ChassisEmulatorProtocol Emulator);
    private sealed record RelayProtocolEntry(IDeviceProtocol Protocol, ModuleRelayControlEmulatorProtocol Emulator);
  }
}
