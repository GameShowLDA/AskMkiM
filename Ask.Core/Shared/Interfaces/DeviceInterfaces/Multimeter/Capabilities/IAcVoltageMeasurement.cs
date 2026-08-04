using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities
{
  /// <summary>
  /// Интерфейс для измерения переменного напряжения.
  /// </summary>
  public interface IAcVoltageMeasurement
  {
    /// <summary>
    /// Устанавливает режим измерения переменного напряжения.
    /// </summary>
    Task<bool> SetACVoltageModeAsync(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Устанавливает диапазон измерения переменного напряжения.
    /// Если значение меньше либо равно 0, используется автоматический выбор диапазона.
    /// </summary>
    /// <param name="range">Максимальный диапазон измерения в вольтах.</param>
    Task<bool> SetACVoltageRangeAsync(double range, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Измеряет переменное напряжение.
    /// </summary>
    /// <param name="param">Ожидаемое значение.</param>
    Task<double> MeasureACVoltageAsync(MeasurementRange measurementRange, IUserInteractionService? userMessageService = null);
  }
}
