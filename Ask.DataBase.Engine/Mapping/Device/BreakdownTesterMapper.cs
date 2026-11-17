using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;

namespace Ask.DataBase.Engine.Mapping.Device;

/// <summary>
/// Маппер для преобразования между <see cref="IBreakdownTester"/> и <see cref="BreakdownTesterDto"/>.
/// Копирует совпадающие свойства через <see cref="ReflectionMapper"/>
/// и отдельно переносит диапазоны напряжений между режимами устройства и DTO.
/// </summary>
public static class BreakdownTesterMapper
{
  /// <summary>
  /// Преобразует реализацию <see cref="IBreakdownTester"/> в DTO <see cref="BreakdownTesterDto"/>.
  /// Копирует совпадающие свойства и создаёт независимые копии диапазонов напряжений режимов.
  /// </summary>
  /// <param name="device">Экземпляр пробойной установки.</param>
  /// <returns>DTO с данными устройства.</returns>
  /// <exception cref="ArgumentNullException">Если device равен null.</exception>
  public static BreakdownTesterDto ToDto(IBreakdownTester device)
  {
    ArgumentNullException.ThrowIfNull(device);
    var dto = ReflectionMapper.Map<IBreakdownTester, BreakdownTesterDto>(device);

    dto.AcwVoltageRange = device.AcwManger.VoltageRange.Clone();
    dto.DcwVoltageRange = device.DcwManger.VoltageRange.Clone();
    dto.IrVoltageRange = device.IrManger.VoltageRange.Clone();
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
    device.AcwManger.VoltageRange = dto.AcwVoltageRange.Clone();
    device.DcwManger.VoltageRange = dto.DcwVoltageRange.Clone();
    device.IrManger.VoltageRange = dto.IrVoltageRange.Clone();
  }
}
