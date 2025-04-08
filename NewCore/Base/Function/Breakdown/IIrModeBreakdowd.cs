using NewCore.Function.GPT.Data;

namespace NewCore.Base.Function.Breakdown
{
  /// <inheritdoc />
  public interface IIrModeBreakdown
  {
    /// <summary>
    /// Устанавливает режим сопротивления изоляции на пробойке.
    /// </summary>
    Task SetModeAsync();

    /// <summary>
    /// Устанавливает напряжения на пробойном устройстве (в В).
    /// </summary>
    /// <param name="value">Устанавливаемое значение.</param>
    Task SetVoltageAsync(double value);

    /// <summary>
    /// Возвращает напряжение на пробойном устройстве.
    /// </summary>
    /// <returns>Значение напряжения (в В).</returns>
    Task<double> GetVoltageAsync();

    /// <summary>
    /// Измерение сопротивления с преобразованием результата в МОм.
    /// </summary>
    /// <param name="param">Ожидаемое значение.</param>
    /// <returns>Результат измерения в МОм.</returns>
    Task<double> MeasureResistanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1);

    /// <summary>
    /// Возвращает список напряжений для заданного сопротивления.
    /// </summary>
    /// <param name="resistance">Сопротивление в МОм.</param>
    /// <returns>Список напряжений.</returns>
    List<int> GetVoltagesForResistance(double resistance);

    /// <summary>
    /// Устанавливает высокий предел сопротивления IR.
    /// </summary>
    /// <param name="value">Устанавливаемое значение (в ГОм).</param>
    Task SetHighResistanceLimitAsync(double value);

    /// <summary>
    /// Устанавливает низкий предел сопротивления IR.
    /// </summary>
    /// <param name="value">Устанавливаемое значение (в МОм).</param>
    Task SetLowResistanceLimitAsync(double value);

    /// <summary>
    /// Устанавливает время теста IR.
    /// </summary>
    /// <param name="value">Устанавливаемое значение (в секундах).</param>
    Task SetTestTimeAsync(double value);

    /// <summary>
    /// Устанавливает смещение IR.
    /// </summary>
    /// <param name="value">Устанавливаемое значение (в ГОм).</param>
    Task SetOffsetAsync(double value);

    /// <summary>
    /// Считывает текущую конфигурацию IR.
    /// </summary>
    /// <returns>Объект с текущими настройками IR.</returns>
    Task<IrConfiguration> ReadConfigurationAsync();
  }
}
