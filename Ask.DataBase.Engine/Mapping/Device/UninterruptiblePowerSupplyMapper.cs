using Ask.Core.Shared.DTO.Devices.UninterruptiblePowerSupply;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;

namespace Ask.DataBase.Engine.Mapping.Device;

/// <summary>
/// Маппер для преобразования между <see cref="IUninterruptiblePowerSupply"/> и 
/// <see cref="UninterruptiblePowerSupplyDto"/>.
/// Использует <see cref="ReflectionMapper"/> для копирования базовых свойств устройства
/// и параметров состояния, таких как путь к устройству.
/// </summary>
public static class UninterruptiblePowerSupplyMapper
{
  /// <summary>
  /// Преобразует реализацию <see cref="IUninterruptiblePowerSupply"/> в DTO 
  /// <see cref="UninterruptiblePowerSupplyDto"/>.
  /// Копирует базовые свойства устройства и путь к устройству.
  /// </summary>
  /// <param name="device">Экземпляр источника бесперебойного питания.</param>
  /// <returns>DTO с данными устройства.</returns>
  /// <exception cref="ArgumentNullException">Если device равен null.</exception>
  public static UninterruptiblePowerSupplyDto ToDto(IUninterruptiblePowerSupply device)
  {
    ArgumentNullException.ThrowIfNull(device);

    var dto = ReflectionMapper.Map<IUninterruptiblePowerSupply, UninterruptiblePowerSupplyDto>(device);

    return dto;
  }

  /// <summary>
  /// Применяет данные из <see cref="UninterruptiblePowerSupplyDto"/> к существующему объекту 
  /// <see cref="IUninterruptiblePowerSupply"/>.
  /// Обновляет базовые свойства устройства и путь к устройству.
  /// </summary>
  /// <param name="device">Экземпляр устройства, в который будут записаны данные.</param>
  /// <param name="dto">Источник данных.</param>
  /// <exception cref="ArgumentNullException">Если device или dto равны null.</exception>
  public static void ApplyDto(IUninterruptiblePowerSupply device, UninterruptiblePowerSupplyDto dto)
  {
    ArgumentNullException.ThrowIfNull(device);
    ArgumentNullException.ThrowIfNull(dto);

    ReflectionMapper.Apply(dto, device);
  }
}