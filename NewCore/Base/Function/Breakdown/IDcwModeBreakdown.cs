using NewCore.Function.GPT.Data;
using Utilities.Interface;

namespace NewCore.Base.Function.Breakdown
{
  /// <summary>
  /// Интерфейс для режима пробоя (DCW Mode Breakdown).
  /// Определяет функциональность, связанную с тестированием пробоя постоянным напряжением.
  /// </summary>
  public interface IDcwModeBreakdown
  {
    #region Mode

    /// <summary>
    /// Устанавливает режим DCW на устройстве.
    /// </summary>
    Task<(bool Success, string Message)> SetModeAsync();

    /// <summary>
    /// Получает текущий режим работы устройства.
    /// </summary>
    Task<string> GetModeAsync();

    #endregion

    #region Voltage

    /// <summary>
    /// Устанавливает напряжение DCW.
    /// </summary>
    /// <param name="value">Значение напряжения (в В).</param>
    Task<(bool Success, string Message)> SetVoltageAsync(double value);

    /// <summary>
    /// Получает текущее установленное напряжение DCW с ППУ.
    /// </summary>
    /// <returns>Значение напряжения в кВ, если удалось получить; иначе — null.</returns>
    Task<double?> GetVoltageAsync();

    #endregion

    #region HighCurrentLimit

    /// <summary>
    /// Устанавливает высокий предел тока DCW.
    /// </summary>
    /// <param name="value">Значение предела (в мА).</param>
    Task<(bool Success, string Message)> SetHighCurrentLimitAsync(double value);

    /// <summary>
    /// Получает установленный верхний предел тока DCW.
    /// </summary>
    Task<double> GetHighCurrentLimitAsync();

    #endregion

    #region LowCurrentLimit

    /// <summary>
    /// Устанавливает низкий предел тока DCW.
    /// </summary>
    /// <param name="value">Значение предела (в мА).</param>
    Task<(bool Success, string Message)> SetLowCurrentLimitAsync(double value);

    /// <summary>
    /// Получает установленный нижний предел тока DCW.
    /// </summary>
    Task<double> GetLowCurrentLimitAsync();

    #endregion

    #region TestTime

    /// <summary>
    /// Устанавливает время теста DCW.
    /// </summary>
    /// <param name="value">Значение времени (в секундах).</param>
    Task<(bool Success, string Message)> SetTestTimeAsync(double value);

    /// <summary>
    /// Получает текущее установленное время теста.
    /// </summary>
    /// <returns>Значение времени в секундах.</returns>
    Task<double> GetTestTimeAsync();

    #endregion

    #region RampTime

    /// <summary>
    /// Устанавливает время нарастания напряжения (Ramp Time) для DCW.
    /// </summary>
    /// <param name="value">Значение времени нарастания в секундах (0.1 – 999.9).</param>
    Task<(bool Success, string Message)> SetRampTimeAsync(double value);

    /// <summary>
    /// Получает текущее время нарастания напряжения (Ramp Time) для текущего теста.
    /// </summary>
    /// <returns>Значение времени нарастания в секундах.</returns>
    Task<double> GetRampTimeAsync();

    #endregion

    #region Offset

    /// <summary>
    /// Устанавливает смещение DCW.
    /// </summary>
    /// <param name="value">Значение смещения (в мА).</param>
    Task<(bool Success, string Message)> SetOffsetAsync(IUserMessageService messageService, double value);

    /// <summary>
    /// Получает текущее значение смещения DCW.
    /// </summary>
    Task<double> GetOffsetAsync();

    #endregion

    #region ArcCurrent

    /// <summary>
    /// Устанавливает значение тока дуги DCW.
    /// </summary>
    /// <param name="value">Значение тока дуги (в мА).</param>
    Task<(bool Success, string Message)> SetArcCurrentAsync(double value);

    /// <summary>
    /// Получает установленный ток дуги DCW.
    /// </summary>
    Task<double> GetArcCurrentAsync();

    #endregion

    #region Конфигурация и измерения

    /// <summary>
    /// Считывает текущую конфигурацию DCW.
    /// </summary>
    /// <returns>Объект <see cref="DcwConfiguration"/> с текущими параметрами.</returns>
    Task<DcwConfiguration> ReadConfigurationAsync();

    /// <summary>
    /// Запускает тест DCW и возвращает результат измерения тока.
    /// </summary>
    /// <returns>Измеренный ток (в мА).</returns>
    Task<double> MeasureCurrentAsync(double param = 0);

    #endregion
  }
}
