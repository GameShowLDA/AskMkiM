using NewCore.Function.GPT.Data;
using static NewCore.Function.GPT.Command.ManualCommandManager;

namespace NewCore.Base.Function.Breakdown
{
  /// <summary>
  /// Интерфейс для режима пробоя (DCW Mode Breakdown).
  /// Определяет функциональность, связанную с тестированием пробоя постоянным напряжением.
  /// </summary>
  public interface IDcwModeBreakdown
  {
    /// <summary>
    /// Устанавливает режим DCW на устройстве.
    /// </summary>
    Task SetModeAsync();

    /// <summary>
    /// Устанавливает напряжение DCW.
    /// </summary>
    /// <param name="value">Значение напряжения (в кВ).</param>
    Task SetVoltageAsync(double value);

    /// <summary>
    /// Устанавливает высокий предел тока DCW.
    /// </summary>
    /// <param name="value">Значение предела (в мА).</param>
    Task SetHighCurrentLimitAsync(double value);

    /// <summary>
    /// Устанавливает низкий предел тока DCW.
    /// </summary>
    /// <param name="value">Значение предела (в мА).</param>
    Task SetLowCurrentLimitAsync(double value);

    /// <summary>
    /// Устанавливает время теста DCW.
    /// </summary>
    /// <param name="value">Значение времени (в секундах).</param>
    Task SetTestTimeAsync(double value);

    /// <summary>
    /// Устанавливает смещение DCW.
    /// </summary>
    /// <param name="value">Значение смещения (в мА).</param>
    Task SetOffsetAsync(double value);

    /// <summary>
    /// Устанавливает значение тока дуги DCW.
    /// </summary>
    /// <param name="value">Значение тока дуги (в мА).</param>
    Task SetArcCurrentAsync(double value);

    /// <summary>
    /// Считывает текущую конфигурацию DCW.
    /// </summary>
    /// <returns>Объект <see cref="DcwConfiguration"/> с текущими параметрами.</returns>
    Task<DcwConfiguration> ReadConfigurationAsync();

    /// <summary>
    /// Запускает тест DCW и возвращает результат измерения тока.
    /// </summary>
    /// <returns>Измеренный ток (в мА).</returns>
    Task<double> MeasureCurrentAsync();
  }
}
