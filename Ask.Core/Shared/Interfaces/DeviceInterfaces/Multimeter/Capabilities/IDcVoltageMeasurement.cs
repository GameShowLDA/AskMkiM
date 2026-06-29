using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

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
    /// Устанавливает режим диапазона измерения напряжения.
    /// </summary>
    /// <param name="mode">Режим диапазона.</param>
    /// <returns></returns>
    Task<bool> SetVoltageRangeAsync(VoltageRange mode, IUserInteractionService? userMessageService = null);
  }
}
