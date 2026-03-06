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
    /// Измеряет переменное напряжение.
    /// </summary>
    /// <param name="param">Ожидаемое значение.</param>
    Task<double> MeasureACVoltageAsync(double param = 0, IUserInteractionService? userMessageService = null);
  }
}
