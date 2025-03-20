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
    /// Устанавливает режим ACW (переменного высокого напряжения).
    /// </summary>
    Task SetModeAsync();

    /// <summary>
    /// Устанавливает напряжение ACW.
    /// </summary>
    /// <param name="value">Напряжение в кВ.</param>
    Task SetVoltageAsync(double value);

    /// <summary>
    /// Устанавливает верхний предел тока ACW.
    /// </summary>
    /// <param name="value">Ток в мА.</param>
    Task SetHighCurrentLimitAsync(double value);

    /// <summary>
    /// Устанавливает нижний предел тока ACW.
    /// </summary>
    /// <param name="value">Ток в мА.</param>
    Task SetLowCurrentLimitAsync(double value);

    /// <summary>
    /// Устанавливает время теста ACW.
    /// </summary>
    /// <param name="value">Время в секундах.</param>
    Task SetTestTimeAsync(double value);

    /// <summary>
    /// Устанавливает частоту ACW.
    /// </summary>
    /// <param name="frequency">Частота (50 или 60 Гц).</param>
    /// <exception cref="ArgumentException">Выбрасывается, если частота не равна 50 или 60 Гц.</exception>
    Task SetFrequencyAsync(int frequency);

    /// <summary>
    /// Устанавливает смещение ACW.
    /// </summary>
    /// <param name="value">Смещение в мА.</param>
    Task SetOffsetAsync(double value);

    /// <summary>
    /// Устанавливает предельное значение тока дугового пробоя ACW.
    /// </summary>
    /// <param name="value">Ток в мА.</param>
    Task SetArcCurrentAsync(double value);

    /// <summary>
    /// Считывает текущую конфигурацию ACW.
    /// </summary>
    /// <returns>Объект <see cref="AcwConfiguration"/> с текущими параметрами.</returns>
    Task<AcwConfiguration> ReadConfigurationAsync();

    /// <summary>
    /// Запускает тест ACW и возвращает измеренный ток.
    /// </summary>
    /// <returns>Измеренное значение тока (в мА).</returns>
    Task<double> MeasureCurrentAsync();
  }
}
