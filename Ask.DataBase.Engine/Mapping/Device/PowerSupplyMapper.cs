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
    dto.MinVoltage = device.VoltageManager.MinVoltage;
    dto.MaxVoltage = device.VoltageManager.MaxVoltage;
    dto.VoltageStep = device.VoltageManager.VoltageStep;
    dto.MinCurrent = device.CurrentManager.MinCurrent;
    dto.MaxCurrent = device.CurrentManager.MaxCurrent;
    dto.CurrentStep = device.CurrentManager.CurrentStep;
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
    ValidateOperatingLimits(dto);
    ReflectionMapper.Apply(dto, device);
    if (device is DeviceWithTcpIp tcp) tcp.ConnectedProfile.Port = dto.Port;
    device.VoltageManager.MinVoltage = dto.MinVoltage;
    device.VoltageManager.MaxVoltage = dto.MaxVoltage;
    device.VoltageManager.VoltageStep = dto.VoltageStep;
    device.CurrentManager.MinCurrent = dto.MinCurrent;
    device.CurrentManager.MaxCurrent = dto.MaxCurrent;
    device.CurrentManager.CurrentStep = dto.CurrentStep;
  }

  /// <summary>
  /// Проверяет диапазоны и шаги уставок источника питания.
  /// </summary>
  /// <param name="dto">Конфигурация источника питания.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Выбрасывается, если диапазон или шаг уставки некорректен.
  /// </exception>
  public static void ValidateOperatingLimits(PowerSupplyDto dto)
  {
    ArgumentNullException.ThrowIfNull(dto);
    ValidateRange(dto.MinVoltage, dto.MaxVoltage, dto.VoltageStep, nameof(dto.VoltageStep));
    ValidateRange(dto.MinCurrent, dto.MaxCurrent, dto.CurrentStep, nameof(dto.CurrentStep));
  }

  private static void ValidateRange(decimal min, decimal max, decimal step, string parameterName)
  {
    if (min < 0 || max <= min || step <= 0 || step > max - min)
    {
      throw new ArgumentOutOfRangeException(parameterName,
        "Минимум должен быть неотрицательным, максимум — больше минимума, а шаг — положительным и не больше диапазона.");
    }
  }
}
