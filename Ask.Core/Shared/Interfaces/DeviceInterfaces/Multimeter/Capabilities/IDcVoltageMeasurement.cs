using Ask.Core.Shared.DTO.Devices.Measurements;
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
    /// <param name="measurementRange">Заданное значение и допустимый диапазон измерения.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="responseDelay">Задержка перед чтением ответа прибора, мс.</param>
    Task<double> MeasureDCVoltageAsync(
      MeasurementRange measurementRange,
      IUserInteractionService? userMessageService = null,
      double responseDelay = 0);

    /// <summary>
    /// Устанавливает диапазон измерения постоянного напряжения.
    /// Если значение меньше либо равно 0, используется автоматический выбор диапазона.
    /// </summary>
    /// <param name="range">Максимальный диапазон измерения в вольтах.</param>
    Task<bool> SetDCVoltageRangeAsync(double range, IUserInteractionService? userMessageService = null);
  }
}
