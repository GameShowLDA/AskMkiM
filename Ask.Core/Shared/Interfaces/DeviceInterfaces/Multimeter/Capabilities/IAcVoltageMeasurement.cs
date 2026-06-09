using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

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
    Task<double> MeasureACVoltageAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Устанавливает режим диапазона измерения напряжения.
    /// </summary>
    /// <param name="mode">Режим диапазона.</param>
    /// <returns></returns>
    Task<bool> SetVoltageRangeAsync(VoltageRange mode, IUserInteractionService? userMessageService = null);
  }
}
