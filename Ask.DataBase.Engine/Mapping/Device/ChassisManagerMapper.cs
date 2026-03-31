using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;

namespace Ask.DataBase.Engine.Mapping.Device;

/// <summary>
/// Маппер для преобразования между <see cref="IChassisManager"/> и <see cref="ChassisManagerDto"/>.
/// Использует <see cref="ReflectionMapper"/> для автоматического копирования базовых свойств
/// и параметров конфигурации, таких как тип шины.
/// </summary>
public static class ChassisManagerMapper
{
  /// <summary>
  /// Преобразует реализацию <see cref="IChassisManager"/> в DTO <see cref="ChassisManagerDto"/>.
  /// Копирует базовые свойства устройства и параметры конфигурации шины.
  /// </summary>
  /// <param name="device">Экземпляр менеджера шасси.</param>
  /// <returns>DTO с данными устройства.</returns>
  /// <exception cref="ArgumentNullException">Если device равен null.</exception>
  public static ChassisManagerDto ToDto(IChassisManager device)
  {
    ArgumentNullException.ThrowIfNull(device);

    var dto = ReflectionMapper.Map<IChassisManager, ChassisManagerDto>(device);

    return dto;
  }

  /// <summary>
  /// Применяет данные из <see cref="ChassisManagerDto"/> к существующему объекту <see cref="IChassisManager"/>.
  /// Обновляет базовые свойства устройства и параметры конфигурации шины.
  /// </summary>
  /// <param name="device">Экземпляр устройства, в который будут записаны данные.</param>
  /// <param name="dto">Источник данных.</param>
  /// <exception cref="ArgumentNullException">Если device или dto равны null.</exception>
  public static void ApplyDto(IChassisManager device, ChassisManagerDto dto)
  {
    ArgumentNullException.ThrowIfNull(device);
    ArgumentNullException.ThrowIfNull(dto);

    ReflectionMapper.Apply(dto, device);
  }
}