using Ask.Core.Shared.DTO.Devices.PowerSupply;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply;
using Ask.Device.Runtime.Base.Device;

namespace Ask.DataBase.Engine.Mapping.Device;

/// <summary>
/// Преобразует конфигурацию внешнего источника питания между DTO и runtime-устройством.
/// </summary>
public static class PowerSupplyMapper
{
  /// <summary>
  /// Создаёт DTO конфигурации источника.
  /// </summary>
  /// <param name="device">Внешний источник питания.</param>
  /// <returns>Конфигурация источника без текущих уставок и состояния выхода.</returns>
  public static PowerSupplyDto ToDto(IPowerSupply device)
  {
    ArgumentNullException.ThrowIfNull(device);
    var dto = ReflectionMapper.Map<IPowerSupply, PowerSupplyDto>(device);
    if (device is DeviceWithTcpIp tcp) dto.Port = tcp.ConnectedProfile.Port;
    return dto;
  }

  /// <summary>
  /// Применяет сохранённую конфигурацию к источнику.
  /// </summary>
  /// <param name="device">Внешний источник питания.</param>
  /// <param name="dto">Сохранённая конфигурация источника.</param>
  public static void ApplyDto(IPowerSupply device, PowerSupplyDto dto)
  {
    ArgumentNullException.ThrowIfNull(device);
    ArgumentNullException.ThrowIfNull(dto);
    if (dto.Port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(dto.Port));
    ReflectionMapper.Apply(dto, device);
    if (device is DeviceWithTcpIp tcp) tcp.ConnectedProfile.Port = dto.Port;
  }
}
