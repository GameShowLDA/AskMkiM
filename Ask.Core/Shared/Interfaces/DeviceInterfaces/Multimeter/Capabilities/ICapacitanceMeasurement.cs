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
    /// <param name="param">Ожиданемео знчение.</param>
    Task<double> MeasureCapacitanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null);
  }
}
