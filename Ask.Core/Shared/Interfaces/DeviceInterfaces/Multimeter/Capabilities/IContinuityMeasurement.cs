using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities
{
  /// <summary>
  /// Интерфейс для проверки проводимости (прозвонки).
  /// </summary>
  public interface IContinuityMeasurement
  {
    /// <summary>
    /// Устанавливает режим прозвонки.
    /// </summary>
    Task<bool> SetContinuityModeAsync(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Проверяет наличие проводимости.
    /// </summary>
    /// <param name="responseDelay">Задержка перед чтением ответа прибора, мс.</param>
    Task<bool> CheckContinuityAsync(bool expectedOutcome, IUserInteractionService? userMessageService = null, double responseDelay = 0);

    /// <summary>
    /// Проверяет наличие проводимости.
    /// </summary>
    /// <param name="measurementRange">Заданное значение и допустимый диапазон измерения.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="responseDelay">Задержка перед чтением ответа прибора, мс.</param>
    Task<double> CheckContinuityAsync(
      MeasurementRange measurementRange,
      IUserInteractionService? userMessageService = null,
      double responseDelay = 0);
  }
}
