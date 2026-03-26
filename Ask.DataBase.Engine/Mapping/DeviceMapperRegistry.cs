using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.DTO.Devices.PowerSourceModule;
using Ask.Core.Shared.DTO.Devices.Rack;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Devices.SwitchingDevice;
using Ask.Core.Shared.DTO.Devices.UninterruptiblePowerSupply;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
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
/// Содержит сопоставление между типами устройств и функциями применения DTO.
/// Позволяет избежать использования switch-case при выборе mapper-а.
/// </summary>
public static class DeviceMapperRegistry
{
  /// <summary>
  /// Словарь сопоставления типа интерфейса устройства и функции применения DTO.
  /// </summary>
  private static readonly Dictionary<Type, Action<IDevice, object>> _map = new()
  {
    { typeof(IFastMeter), (d, dto) => FastMeterMapper.ApplyDto((IFastMeter)d, (FastMeterDto)dto) },
    { typeof(IRelaySwitchModule), (d, dto) => RelaySwitchModuleMapper.ApplyDto((IRelaySwitchModule)d, (RelaySwitchModuleDto)dto) },
    { typeof(IPowerSourceModule), (d, dto) => PowerSourceModuleMapper.ApplyDto((IPowerSourceModule)d, (PowerSourceModuleDto)dto) },
    { typeof(ISwitchingDevice), (d, dto) => SwitchingDeviceMapper.ApplyDto((ISwitchingDevice)d, (SwitchingDeviceDto)dto) },
    { typeof(IUninterruptiblePowerSupply), (d, dto) => UninterruptiblePowerSupplyMapper.ApplyDto((IUninterruptiblePowerSupply)d, (UninterruptiblePowerSupplyDto)dto) },
    { typeof(IChassisManager), (d, dto) => ChassisManagerMapper.ApplyDto((IChassisManager)d, (ChassisManagerDto)dto) },
    { typeof(IRack), (d, dto) => RackMapper.ApplyDto((IRack)d, (RackDto)dto) },
  };

  /// <summary>
  /// Применяет DTO к устройству, автоматически определяя нужный mapper.
  /// </summary>
  /// <param name="device">Экземпляр устройства.</param>
  /// <param name="dto">DTO с данными.</param>
  /// <exception cref="NotSupportedException">
  /// Выбрасывается, если для типа устройства не найден mapper.
  /// </exception>
  public static void Apply(IDevice device, object dto)
  {
    var interfaceType = device.GetType()
      .GetInterfaces()
      .FirstOrDefault(i => _map.ContainsKey(i));

    if (interfaceType == null)
      throw new NotSupportedException($"Mapper not found for {device.GetType().Name}");

    _map[interfaceType](device, dto);
  }
}