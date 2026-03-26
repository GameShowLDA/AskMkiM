using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;

namespace Ask.DataBase.Engine.Mapping.Device;

/// <summary>
/// Маппер для преобразования между <see cref="IRelaySwitchModule"/> и <see cref="RelaySwitchModuleDto"/>.
/// Использует <see cref="ReflectionMapper"/> для автоматического копирования совпадающих свойств,
/// таких как параметры коммутации, характеристики и базовые данные устройства.
/// </summary>
public static class RelaySwitchModuleMapper
{
  /// <summary>
  /// Преобразует реализацию <see cref="IRelaySwitchModule"/> в DTO <see cref="RelaySwitchModuleDto"/>.
  /// Копирует все совпадающие свойства, включая характеристики коммутации и конфигурацию модуля.
  /// </summary>
  /// <param name="device">Экземпляр модуля коммутации реле.</param>
  /// <returns>DTO с данными устройства.</returns>
  /// <exception cref="ArgumentNullException">Если device равен null.</exception>
  public static RelaySwitchModuleDto ToDto(IRelaySwitchModule device)
  {
    ArgumentNullException.ThrowIfNull(device);

    var dto = ReflectionMapper.Map<IRelaySwitchModule, RelaySwitchModuleDto>(device);

    // Кастомная логика, при необходимости

    return dto;
  }

  /// <summary>
  /// Применяет данные из <see cref="RelaySwitchModuleDto"/> к существующему объекту <see cref="IRelaySwitchModule"/>.
  /// Обновляет совпадающие свойства, такие как параметры шины, сопротивление и ёмкость.
  /// </summary>
  /// <param name="device">Экземпляр устройства, в который будут записаны данные.</param>
  /// <param name="dto">Источник данных.</param>
  /// <exception cref="ArgumentNullException">Если device или dto равны null.</exception>
  public static void ApplyDto(IRelaySwitchModule device, RelaySwitchModuleDto dto)
  {
    ArgumentNullException.ThrowIfNull(device);
    ArgumentNullException.ThrowIfNull(dto);

    ReflectionMapper.Apply(dto, device);

    // Кастомная логика, при необходимости
  }
}