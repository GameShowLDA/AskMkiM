using Ask.Core.Shared.DTO.Devices.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;

namespace Ask.DataBase.Engine.Mapping.Device;

/// <summary>
/// Маппер для преобразования между <see cref="IPowerSourceModule"/> и <see cref="PowerSourceModuleDto"/>.
/// Использует <see cref="ReflectionMapper"/> для автоматического копирования совпадающих свойств,
/// включая базовые параметры устройства и калибровочные данные.
/// </summary>
public static class PowerSourceModuleMapper
{
  /// <summary>
  /// Преобразует реализацию <see cref="IPowerSourceModule"/> в DTO <see cref="PowerSourceModuleDto"/>.
  /// Копирует все совпадающие свойства, включая параметры подключения и калибровочные данные.
  /// </summary>
  /// <param name="device">Экземпляр модуля источника питания.</param>
  /// <returns>DTO с данными устройства.</returns>
  /// <exception cref="ArgumentNullException">Если device равен null.</exception>
  public static PowerSourceModuleDto ToDto(IPowerSourceModule device)
  {
    ArgumentNullException.ThrowIfNull(device);

    var dto = ReflectionMapper.Map<IPowerSourceModule, PowerSourceModuleDto>(device);

    // Кастомная логика, при необходимости

    return dto;
  }

  /// <summary>
  /// Применяет данные из <see cref="PowerSourceModuleDto"/> к существующему объекту <see cref="IPowerSourceModule"/>.
  /// Обновляет совпадающие свойства, включая калибровочные параметры.
  /// </summary>
  /// <param name="device">Экземпляр устройства, в который будут записаны данные.</param>
  /// <param name="dto">Источник данных.</param>
  /// <exception cref="ArgumentNullException">Если device или dto равны null.</exception>
  public static void ApplyDto(IPowerSourceModule device, PowerSourceModuleDto dto)
  {
    ArgumentNullException.ThrowIfNull(device);
    ArgumentNullException.ThrowIfNull(dto);

    ReflectionMapper.Apply(dto, device);

    // Кастомная логика, при необходимости
  }
}