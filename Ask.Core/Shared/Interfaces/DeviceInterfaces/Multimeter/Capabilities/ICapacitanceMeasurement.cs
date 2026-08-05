using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities
{
  /// <summary>
  /// Интерфейс для измерения ёмкости.
  /// </summary>
  public interface ICapacitanceMeasurement
  {
    /// <summary>
    /// Устанавливает режим измерения ёмкости.
    /// </summary>
    Task<bool> SetCapacitanceModeAsync(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Устанавливает диапазон измерения ёмкости.
    /// Значение меньше или равное нулю включает автоматический выбор диапазона.
    /// </summary>
    /// <param name="range">Диапазон измерения в нФ.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>Задача, возвращающая результат установки диапазона.</returns>
    Task<bool> SetCapacitanceRangeAsync(double range, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Выполняет измерение ёмкости.
    /// </summary>
    /// <param name="measurementRange">Заданное значение и допустимый диапазон измерения.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="measurementCount">Количество положительных результатов для усреднения.</param>
    /// <param name="responseDelay">Задержка перед чтением ответа прибора, мс.</param>
    /// <returns>Среднее значение положительных результатов измерений.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Выбрасывается, если <paramref name="measurementCount"/> меньше единицы.
    /// </exception>
    Task<double> MeasureCapacitanceAsync(
      MeasurementRange measurementRange,
      IUserInteractionService? userMessageService = null,
      int measurementCount = 5,
      double responseDelay = 0);
  }
}
