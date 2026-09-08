using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

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
    Task<BreakdownMeasurementResponse> MeasureAsync(ElectricalTestFunction electricalTestFunction, MeasurementRange measurementRange, bool waitFullTime = false, IUserInteractionService? userMessageService = null);

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
