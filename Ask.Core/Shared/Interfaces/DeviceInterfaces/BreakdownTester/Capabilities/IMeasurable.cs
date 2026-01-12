using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities
{
  /// <summary>
  /// Интерфейс для режимов, поддерживающих выполнение измерений (тока, напряжения, сопротивления и др.).
  /// </summary>
  public interface IMeasurable
  {
    /// <summary>
    /// Выполняет измерение физического параметра (тока, напряжения, сопротивления и др.).
    /// </summary>
    /// <param name="param">
    /// Ожидаемое значение (может использоваться для проверки точности или предварительной настройки).
    /// </param>
    /// <param name="rangeFrom">
    /// Нижняя граница диапазона измерений (опционально, по умолчанию -1 означает "не задано").
    /// </param>
    /// <param name="rangeTo">
    /// Верхняя граница диапазона измерений (опционально, по умолчанию -1 означает "не задано").
    /// </param>
    /// <param name="userMessageService">
    /// (Необязательно) Сервис для отображения сообщений пользователю.  
    /// Может быть <c>null</c>, если сообщения выводить не требуется.
    /// </param>
    /// <returns>
    /// Числовое значение результата измерения (единицы зависят от конкретного режима устройства).
    /// </returns>
    Task<(double value, string unit)> MeasureAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, bool waitFullTime = false, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Останавливает запущенный тест.
    /// </summary>
    Task StopMeasure();

    /// <summary>
    /// Применяет напряжение без немедленного выполнения измерения.
    /// </summary>
    Task ApplyVoltageAsync(IUserInteractionService? userMessageService = null);
  }
}
