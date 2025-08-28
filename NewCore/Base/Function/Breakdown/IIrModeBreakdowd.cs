using NewCore.Function.GPT.Data;

namespace NewCore.Base.Function.Breakdown
{
  /// <summary>
  /// Интерфейс для режима измерения сопротивления изоляции (IR).
  /// </summary>
  public interface IIrModeBreakdown
  {
    #region Mode

    /// <summary>
    /// Устанавливает режим сопротивления изоляции на пробойке.
    /// </summary>
    Task<(bool Success, string Message)> SetModeAsync();

    /// <summary>
    /// Возвращает текущий режим работы.
    /// </summary>
    Task<(bool Success, string Message)> GetModeAsync();

    #endregion

    #region Voltage

    /// <summary>
    /// Устанавливает напряжение на пробойном устройстве (в В).
    /// </summary>
    /// <param name="value">Устанавливаемое значение.</param>
    Task<(bool Success, string Message)> SetVoltageAsync(double value);

    /// <summary>
    /// Возвращает напряжение на пробойном устройстве.
    /// </summary>
    /// <returns>Значение напряжения (в В).</returns>
    Task<double> GetVoltageAsync();

    #endregion

    #region HighResistanceLimit

    /// <summary>
    /// Устанавливает высокий предел сопротивления IR.
    /// </summary>
    /// <param name="value">Устанавливаемое значение (в ГОм).</param>
    Task<(bool Success, string Message)> SetHighResistanceLimitAsync(double value);

    /// <summary>
    /// Возвращает установленный верхний предел сопротивления IR.
    /// </summary>
    Task<double> GetHighResistanceLimitAsync();

    #endregion

    #region LowResistanceLimit

    /// <summary>
    /// Устанавливает низкий предел сопротивления IR.
    /// </summary>
    /// <param name="value">Устанавливаемое значение (в МОм).</param>
    Task<(bool Success, string Message)> SetLowResistanceLimitAsync(double value);

    /// <summary>
    /// Возвращает установленный нижний предел сопротивления IR.
    /// </summary>
    Task<double> GetLowResistanceLimitAsync();

    #endregion

    #region TestTime

    /// <summary>
    /// Устанавливает время теста IR.
    /// </summary>
    /// <param name="value">Устанавливаемое значение (в секундах).</param>
    Task<(bool Success, string Message)> SetTestTimeAsync(double value);

    /// <summary>
    /// Возвращает текущее установленное время теста IR.
    /// </summary>
    Task<double> GetTestTimeAsync();

    #endregion

    #region Offset

    /// <summary>
    /// Устанавливает смещение IR.
    /// </summary>
    /// <param name="value">Устанавливаемое значение (в ГОм).</param>
    Task<(bool Success, string Message)> SetOffsetAsync(double value);

    /// <summary>
    /// Возвращает текущее установленное смещение IR.
    /// </summary>
    Task<double> GetOffsetAsync();

    #endregion

    #region Измерение и конфигурация

    /// <summary>
    /// Измерение сопротивления с преобразованием результата в МОм.
    /// </summary>
    /// <param name="param">Ожидаемое значение.</param>
    /// <param name="rangeFrom">Диапазон от (опционально).</param>
    /// <param name="rangeTo">Диапазон до (опционально).</param>
    /// <returns>Результат измерения в МОм.</returns>
    Task<double> MeasureResistanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1);

    /// <summary>
    /// Возвращает список напряжений для заданного сопротивления.
    /// </summary>
    /// <param name="resistance">Сопротивление в МОм.</param>
    /// <returns>Список напряжений.</returns>
    List<int> GetVoltagesForResistance(double resistance);

    /// <summary>
    /// Считывает текущую конфигурацию IR.
    /// </summary>
    /// <returns>Объект с текущими настройками IR.</returns>
    Task<IrConfiguration> ReadConfigurationAsync();

    Task StopMeasure();

    #endregion
  }
}
