using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities
{
  /// <summary>
  /// Определяет интерфейс для измерения сопротивления, включая установку режима измерения и выполнение измерения.
  /// </summary>
  public interface IResistanceMeasurement
  {
    /// <summary>
    /// Асинхронно устанавливает режим измерения сопротивления.
    /// </summary>
    /// <returns>Задача, завершающаяся после установки режима.</returns>
    Task<bool> SetResistanceModeAsync(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Асинхронно устанавливает диапазон измерения сопротивления.
    /// Значение меньше или равное нулю включает автоматический выбор диапазона.
    /// </summary>
    /// <param name="range">Диапазон измерения в Омах.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>Задача, возвращающая результат установки диапазона.</returns>
    Task<bool> SetResistanceRangeAsync(double range, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Асинхронно выполняет измерение сопротивления по правилу двух правильных
    /// и одного допустимого ложного результата.
    /// </summary>
    /// <returns>Задача, возвращающая измеренное значение сопротивления в Омах.</returns>
    /// <param name="measurementRange">Ожидаемое значение и допустимые границы результата.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="responseDelay">Задержка перед чтением ответа прибора, мс.</param>
    Task<double> MeasureResistanceAsync(
      MeasurementRange measurementRange,
      IUserInteractionService? userMessageService = null,
      double responseDelay = 0);

    /// <summary>
    /// Асинхронно выполняет серию измерений сопротивления с заданным правилом приёмки.
    /// </summary>
    /// <returns>Задача, возвращающая итоговое значение сопротивления в Омах.</returns>
    /// <param name="measurementRange">Ожидаемое значение и допустимые границы результата.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="correctMeasurementCount">Обязательное количество результатов в допустимых границах.</param>
    /// <param name="falseMeasurementCount">Допустимое количество результатов вне допустимых границ.</param>
    /// <param name="responseDelay">Задержка перед чтением ответа прибора, мс.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Выбрасывается, если <paramref name="correctMeasurementCount"/> меньше единицы
    /// или <paramref name="falseMeasurementCount"/> меньше нуля.
    /// </exception>
    Task<double> MeasureResistanceAsync(
      MeasurementRange measurementRange,
      IUserInteractionService? userMessageService,
      int correctMeasurementCount,
      int falseMeasurementCount,
      double responseDelay = 0);
  }
}
