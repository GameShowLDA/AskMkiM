using Ask.Core.Shared.DTO.Devices.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;

namespace Ask.DataBase.Engine.Mapping.Device;

/// <summary>
/// Маппер для преобразования между <see cref="ISwitchingDevice"/> и <see cref="SwitchingDeviceDto"/>.
/// Использует <see cref="ReflectionMapper"/> для копирования базовых свойств устройства.
/// Поскольку устройство не содержит собственных параметров (только поведение через менеджеры),
/// маппинг ограничивается базовыми данными.
/// </summary>
public static class SwitchingDeviceMapper
{
  /// <summary>
  /// Преобразует реализацию <see cref="ISwitchingDevice"/> в DTO <see cref="SwitchingDeviceDto"/>.
  /// Копирует базовые свойства устройства (идентификаторы, подключение, класс и т.д.).
  /// </summary>
  /// <param name="device">Экземпляр устройства коммутации.</param>
  /// <returns>DTO с данными устройства.</returns>
  /// <exception cref="ArgumentNullException">Если device равен null.</exception>
  public static SwitchingDeviceDto ToDto(ISwitchingDevice device)
  {
    ArgumentNullException.ThrowIfNull(device);

    var dto = ReflectionMapper.Map<ISwitchingDevice, SwitchingDeviceDto>(device);

    return dto;
  }

  /// <summary>
  /// Применяет данные из <see cref="SwitchingDeviceDto"/> к существующему объекту <see cref="ISwitchingDevice"/>.
  /// Обновляет только базовые свойства устройства.
  /// </summary>
  /// <param name="device">Экземпляр устройства, в который будут записаны данные.</param>
  /// <param name="dto">Источник данных.</param>
  /// <exception cref="ArgumentNullException">Если device или dto равны null.</exception>
  public static void ApplyDto(ISwitchingDevice device, SwitchingDeviceDto dto)
  {
    ArgumentNullException.ThrowIfNull(device);
    ArgumentNullException.ThrowIfNull(dto);

    ReflectionMapper.Apply(dto, device);
  }
}