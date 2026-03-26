using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.DTO.Devices.PowerSourceModule;
using Ask.Core.Shared.DTO.Devices.Rack;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Devices.SwitchingDevice;
using Ask.Core.Shared.DTO.Devices.UninterruptiblePowerSupply;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Rack;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.DataBase.Engine.Mapping.Device;

namespace Ask.DataBase.Engine.Mapping;

/// <summary>
/// Реестр мапперов устройств.
/// Содержит сопоставление между интерфейсом runtime-устройства и DTO-моделью.
/// </summary>
public static class DeviceMapperRegistry
{
  private static readonly Dictionary<Type, Action<IDevice, object>> MapActions = new()
  {
    { typeof(IBreakdownTester), (device, dto) => BreakdownTesterMapper.ApplyDto((IBreakdownTester)device, (BreakdownTesterDto)dto) },
    { typeof(IFastMeter), (device, dto) => FastMeterMapper.ApplyDto((IFastMeter)device, (FastMeterDto)dto) },
    { typeof(IRelaySwitchModule), (device, dto) => RelaySwitchModuleMapper.ApplyDto((IRelaySwitchModule)device, (RelaySwitchModuleDto)dto) },
    { typeof(IPowerSourceModule), (device, dto) => PowerSourceModuleMapper.ApplyDto((IPowerSourceModule)device, (PowerSourceModuleDto)dto) },
    { typeof(ISwitchingDevice), (device, dto) => SwitchingDeviceMapper.ApplyDto((ISwitchingDevice)device, (SwitchingDeviceDto)dto) },
    { typeof(IUninterruptiblePowerSupply), (device, dto) => UninterruptiblePowerSupplyMapper.ApplyDto((IUninterruptiblePowerSupply)device, (UninterruptiblePowerSupplyDto)dto) },
    { typeof(IChassisManager), (device, dto) => ChassisManagerMapper.ApplyDto((IChassisManager)device, (ChassisManagerDto)dto) },
    { typeof(IRack), (device, dto) => RackMapper.ApplyDto((IRack)device, (RackDto)dto) },
  };

  /// <summary>
  /// Применяет DTO к runtime-устройству, автоматически определяя нужный маппер.
  /// </summary>
  /// <param name="device">Экземпляр runtime-устройства.</param>
  /// <param name="dto">DTO с данными устройства.</param>
  public static void Apply(IDevice device, object dto)
  {
    ArgumentNullException.ThrowIfNull(device);
    ArgumentNullException.ThrowIfNull(dto);

    var interfaceType = device.GetType()
      .GetInterfaces()
      .FirstOrDefault(MapActions.ContainsKey);

    if (interfaceType == null)
    {
      throw new NotSupportedException(
        $"Для типа '{device.GetType().Name}' не найден mapper.");
    }

    MapActions[interfaceType](device, dto);
  }
}
