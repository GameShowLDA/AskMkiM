using NewCore.Function.GPT.Data;
using static NewCore.Function.GPT.Command.ManualCommandManager;

namespace NewCore.Base.Function.Breakdown
{
  /// <summary>
  /// Управление режимом ACW на пробойной установке.
  /// </summary>
  public interface IAcwModeBreakdown
  {
    /// <summary>
    /// Устанавливает режим ACW и проверяет, что устройство приняло значение.
    /// </summary>
    /// <returns>Кортеж: bool — успех, string — сообщение об ошибке (если есть).</returns>
    Task<(bool Success, string Message)> SetModeAsync();

    /// <summary>
    /// Устанавливает напряжение ACW и проверяет, что устройство приняло значение.
    /// </summary>
    /// <param name="value">Значение напряжения в вольтах.</param>
    /// <returns>Кортеж: bool — успех, string — сообщение об ошибке (если есть).</returns>
    Task<(bool Success, string Message)> SetVoltageAsync(double value);

    /// <summary>
    /// Устанавливает верхний предел тока ACW и проверяет, что устройство приняло значение.
    /// </summary>
    /// <param name="value">Ток в миллиамперах.</param>
    /// <returns>Кортеж: bool — успех, string — сообщение об ошибке (если есть).</returns>
    Task<(bool Success, string Message)> SetHighCurrentLimitAsync(double value);

    /// <summary>
    /// Устанавливает нижний предел тока ACW и проверяет, что устройство приняло значение.
    /// </summary>
    /// <param name="value">Ток в миллиамперах.</param>
    /// <returns>Кортеж: bool — успех, string — сообщение об ошибке (если есть).</returns>
    Task<(bool Success, string Message)> SetLowCurrentLimitAsync(double value);

    /// <summary>
    /// Устанавливает время теста ACW и проверяет, что устройство приняло значение.
    /// </summary>
    /// <param name="value">Время в секундах.</param>
    /// <returns>Кортеж: bool — успех, string — сообщение об ошибке (если есть).</returns>
    Task<(bool Success, string Message)> SetTestTimeAsync(double value);

    /// <summary>
    /// Устанавливает время нарастания напряжения ACW (Ramp Time) и проверяет, что устройство приняло значение.
    /// </summary>
    /// <param name="value">Время в секундах.</param>
    /// <returns>Кортеж: bool — успех, string — сообщение об ошибке (если есть).</returns>
    Task<(bool Success, string Message)> SetRampTimeAsync(double value);

    /// <summary>
    /// Устанавливает частоту ACW и проверяет, что устройство приняло значение.
    /// </summary>
    /// <param name="frequency">Частота (допустимые значения: 50 или 60 Гц).</param>
    /// <returns>Кортеж: bool — успех, string — сообщение об ошибке (если есть).</returns>
    /// <exception cref="ArgumentException">Если частота не равна 50 или 60.</exception>
    Task<(bool Success, string Message)> SetFrequencyAsync(int frequency);

    /// <summary>
    /// Устанавливает смещение ACW и проверяет, что устройство приняло значение.
    /// </summary>
    /// <param name="value">Смещение в мА.</param>
    /// <returns>Кортеж: bool — успех, string — сообщение об ошибке (если есть).</returns>
    Task<(bool Success, string Message)> SetOffsetAsync(double value);

    /// <summary>
    /// Устанавливает предельное значение тока дугового пробоя ACW и проверяет, что устройство приняло значение.
    /// </summary>
    /// <param name="value">Ток в мА.</param>
    /// <returns>Кортеж: bool — успех, string — сообщение об ошибке (если есть).</returns>
    Task<(bool Success, string Message)> SetArcCurrentAsync(double value);

    /// <summary>
    /// Получает текущий режим ACW (переменного высокого напряжения).
    /// </summary>
    Task<string> GetModeAsync();

    /// <summary>
    /// Получает установленное значение напряжения ACW.
    /// </summary>
    /// <returns>Напряжение в кВ.</returns>
    Task<double> GetVoltageAsync();

    /// <summary>
    /// Получает установленное верхнее значение предела тока ACW.
    /// </summary>
    /// <returns>Ток в мА.</returns>
    Task<double> GetHighCurrentLimitAsync();

    /// <summary>
    /// Получает установленное нижнее значение предела тока ACW.
    /// </summary>
    /// <returns>Ток в мА.</returns>
    Task<double> GetLowCurrentLimitAsync();

    /// <summary>
    /// Получает установленное время теста ACW.
    /// </summary>
    /// <returns>Время в секундах.</returns>
    Task<double> GetTestTimeAsync();

    /// <summary>
    /// Получает установленное значение времени нарастания напряжения (Ramp Time) для DCW.
    /// </summary>
    /// <returns>Значение времени нарастания в секундах.</returns>
    Task<double> GetRampTimeAsync();

    /// <summary>
    /// Получает установленную частоту ACW.
    /// </summary>
    /// <returns>Частота (50 или 60 Гц).</returns>
    Task<int> GetFrequencyAsync();

    /// <summary>
    /// Получает установленное значение смещения ACW.
    /// </summary>
    /// <returns>Смещение в мА.</returns>
    Task<double> GetOffsetAsync();

    /// <summary>
    /// Получает установленное предельное значение тока дугового пробоя ACW.
    /// </summary>
    /// <returns>Ток в мА.</returns>
    Task<double> GetArcCurrentAsync();

    /// <summary>
    /// Считывает текущую конфигурацию ACW.
    /// </summary>
    /// <returns>Объект <see cref="AcwConfiguration"/> с текущими параметрами.</returns>
    Task<AcwConfiguration> ReadConfigurationAsync();

    /// <summary>
    /// Запускает тест ACW и возвращает измеренный ток.
    /// </summary>
    /// <returns>Измеренное значение тока (в мА).</returns>
    /// <param name="param">Ожидаемое значение.</param>
    Task<double> MeasureCurrentAsync(double param = 0);
  }
}
