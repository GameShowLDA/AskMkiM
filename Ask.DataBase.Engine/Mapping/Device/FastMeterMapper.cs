using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;

namespace Ask.DataBase.Engine.Mapping.Device;

/// <summary>
/// Маппер для преобразования между <see cref="IFastMeter"/> и <see cref="FastMeterDto"/>.
/// Использует универсальный <see cref="ReflectionMapper"/> для копирования совпадающих свойств,
/// а также позволяет добавлять кастомную логику при необходимости.
/// </summary>
public static class FastMeterMapper
{
  /// <summary>
  /// Преобразует реализацию <see cref="IFastMeter"/> в DTO <see cref="FastMeterDto"/>.
  /// Копирует все совпадающие свойства (Id, Name, режимы и т.д.).
  /// </summary>
  /// <param name="device">Экземпляр устройства быстрого измерителя.</param>
  /// <returns>DTO с данными устройства.</returns>
  /// <exception cref="ArgumentNullException">Если device равен null.</exception>
  public static FastMeterDto ToDto(IFastMeter device)
  {
    ArgumentNullException.ThrowIfNull(device);

    var dto = ReflectionMapper.Map<IFastMeter, FastMeterDto>(device);

    // Кастомная логика, при необходимости
    return dto;
  }

  /// <summary>
  /// Применяет данные из <see cref="FastMeterDto"/> к существующему объекту <see cref="IFastMeter"/>.
  /// Обновляет только совпадающие свойства.
  /// </summary>
  /// <param name="device">Экземпляр устройства, в который будут записаны данные.</param>
  /// <param name="dto">Источник данных.</param>
  /// <exception cref="ArgumentNullException">Если device или dto равны null.</exception>
  public static void ApplyDto(IFastMeter device, FastMeterDto dto)
  {
    ArgumentNullException.ThrowIfNull(device);
    ArgumentNullException.ThrowIfNull(dto);

    ReflectionMapper.Apply(dto, device);

    // Кастомная логика, при необходимости
  }
}