using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities
{
  /// <summary>
  /// Интерфейс для измерения постоянного напряжения.
  /// </summary>
  public interface IDcVoltageMeasurement
  {
    /// <summary>
    /// Устанавливает режим измерения постоянного напряжения.
    /// </summary>
    Task<bool> SetDCVoltageModeAsync(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Измеряет постоянное напряжение.
    /// </summary>
    /// <param name="param">Ожидаемое значение.</param>
    Task<double> MeasureDCVoltageAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Устанавливает диапазон измерения постоянного напряжения.
    /// Если значение меньше либо равно 0, используется автоматический выбор диапазона.
    /// </summary>
    /// <param name="range">Максимальный диапазон измерения в вольтах.</param>
    Task<bool> SetDCVoltageRangeAsync(double range, IUserInteractionService? userMessageService = null);
  }
}
