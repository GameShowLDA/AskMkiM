using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities
{
  /// <summary>
  /// Интерфейс для работы мультиметра в режиме проверки диода.
  /// </summary>
  public interface IDiodeMeasurement
  {
    /// <summary>
    /// Устанавливает режим проверки диода.
    /// </summary>
    Task<bool> SetDiodeModeAsync(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Проверяет диод и возвращает измеренное падение напряжения.
    /// </summary>
    /// <param name="param">Ожидаемое значение.</param>
    Task<double> CheckDiodeAsync(MeasurementRange measurementRange, IUserInteractionService? userMessageService = null);
  }
}
