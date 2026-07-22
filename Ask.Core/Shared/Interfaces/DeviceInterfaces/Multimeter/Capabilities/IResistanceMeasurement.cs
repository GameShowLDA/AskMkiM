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
    /// Асинхронно выполняет измерение сопротивления.
    /// </summary>
    /// <returns>Задача, возвращающая измеренное значение сопротивления в Омах.</returns>
    /// <param name="param">Ожидаемое значение.</param>
    /// <param name="responseDelay">Задержка перед чтением ответа прибора, мс.</param>
    Task<double> MeasureResistanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null, double responseDelay = 0);
  }
}
