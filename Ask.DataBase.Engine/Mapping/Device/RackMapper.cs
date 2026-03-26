using Ask.Core.Shared.DTO.Devices.Rack;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Rack;

namespace Ask.DataBase.Engine.Mapping.Device;

/// <summary>
/// Маппер для преобразования между <see cref="IRack"/> и <see cref="RackDto"/>.
/// Использует <see cref="ReflectionMapper"/> для копирования базовых свойств устройства.
/// Поскольку стойка не содержит дополнительных сохраняемых параметров,
/// маппинг ограничивается базовыми данными.
/// </summary>
public static class RackMapper
{
  /// <summary>
  /// Преобразует реализацию <see cref="IRack"/> в DTO <see cref="RackDto"/>.
  /// Копирует базовые свойства устройства (идентификаторы, подключение, класс и т.д.).
  /// </summary>
  /// <param name="device">Экземпляр коммутационной стойки.</param>
  /// <returns>DTO с данными устройства.</returns>
  /// <exception cref="ArgumentNullException">Если device равен null.</exception>
  public static RackDto ToDto(IRack device)
  {
    ArgumentNullException.ThrowIfNull(device);

    var dto = ReflectionMapper.Map<IRack, RackDto>(device);

    return dto;
  }

  /// <summary>
  /// Применяет данные из <see cref="RackDto"/> к существующему объекту <see cref="IRack"/>.
  /// Обновляет только базовые свойства устройства.
  /// </summary>
  /// <param name="device">Экземпляр устройства, в который будут записаны данные.</param>
  /// <param name="dto">Источник данных.</param>
  /// <exception cref="ArgumentNullException">Если device или dto равны null.</exception>
  public static void ApplyDto(IRack device, RackDto dto)
  {
    ArgumentNullException.ThrowIfNull(device);
    ArgumentNullException.ThrowIfNull(dto);

    ReflectionMapper.Apply(dto, device);
  }
}