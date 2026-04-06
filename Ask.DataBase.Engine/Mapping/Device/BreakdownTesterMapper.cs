using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;

namespace Ask.DataBase.Engine.Mapping.Device;

/// <summary>
/// Маппер для преобразования между <see cref="IBreakdownTester"/> и <see cref="BreakdownTesterDto"/>.
/// Использует <see cref="ReflectionMapper"/> для автоматического копирования всех совпадающих свойств,
/// включая параметры режимов и ограничения напряжений.
/// </summary>
public static class BreakdownTesterMapper
{
  /// <summary>
  /// Преобразует реализацию <see cref="IBreakdownTester"/> в DTO <see cref="BreakdownTesterDto"/>.
  /// Копирует все совпадающие свойства, включая режим работы и параметры напряжения.
  /// </summary>
  /// <param name="device">Экземпляр пробойной установки.</param>
  /// <returns>DTO с данными устройства.</returns>
  /// <exception cref="ArgumentNullException">Если device равен null.</exception>
  public static BreakdownTesterDto ToDto(IBreakdownTester device)
  {
    ArgumentNullException.ThrowIfNull(device);
    var dto = ReflectionMapper.Map<IBreakdownTester, BreakdownTesterDto>(device);

    return dto;
  }

  /// <summary>
  /// Применяет данные из <see cref="BreakdownTesterDto"/> к существующему объекту <see cref="IBreakdownTester"/>.
  /// Обновляет режим работы и параметры напряжения устройства.
  /// </summary>
  /// <param name="device">Экземпляр устройства, в который будут записаны данные.</param>
  /// <param name="dto">Источник данных.</param>
  /// <exception cref="ArgumentNullException">Если device или dto равны null.</exception>
  public static void ApplyDto(IBreakdownTester device, BreakdownTesterDto dto)
  {
    ArgumentNullException.ThrowIfNull(device);
    ArgumentNullException.ThrowIfNull(dto);

    ReflectionMapper.Apply(dto, device);
  }
}