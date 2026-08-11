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
    Task<bool> SetDiodeModeAsync(
      IUserInteractionService? userMessageService = null,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет диод и возвращает измеренное падение напряжения.
    /// </summary>
    /// <param name="measurementRange">Заданное значение и допустимый диапазон измерения.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="responseDelay">Задержка перед чтением ответа прибора, мс.</param>
    Task<double> CheckDiodeAsync(
      MeasurementRange measurementRange,
      IUserInteractionService? userMessageService = null,
      double responseDelay = 0,
      CancellationToken cancellationToken = default);
  }
}
