using NewCore.Function.GPT.Data;

namespace NewCore.Base.Function.Breakdown
{
  /// <summary>
  /// Управление системными настройками на пробойной установке.
  /// </summary>
  public interface ISystemSettingsBreakdown
  {
    /// <summary>
    /// Устанавливает контрастность дисплея (от 1 до 8).
    /// </summary>
    /// <param name="value">Значение контрастности (1-8).</param>
    Task SetLcdContrastAsync(double value);

    /// <summary>
    /// Устанавливает яркость дисплея (1 - темный, 2 - яркий).
    /// </summary>
    /// <param name="value">Значение яркости (1 или 2).</param>
    Task SetLcdBrightnessAsync(double value);

    /// <summary>
    /// Включает/выключает звук успешного теста.
    /// </summary>
    /// <param name="state">Состояние (ON или OFF).</param>
    Task SetBuzzerPrimarySound(bool state);

    /// <summary>
    /// Включает/выключает звук ошибочного теста.
    /// </summary>
    /// <param name="state">Состояние (ON или OFF).</param>
    Task SetBuzzerFeedbackSound(bool state);

    /// <summary>
    /// Устанавливает продолжительность звука успешного теста (0.2 - 999.9 секунд).
    /// </summary>
    /// <param name="duration">Длительность сигнала (0.2 - 999.9).</param>
    Task SetBuzzerPrimaryTime(double duration);

    /// <summary>
    /// Устанавливает продолжительность звука ошибочного теста (0.2 - 999.9 секунд).
    /// </summary>
    /// <param name="duration">Длительность сигнала (0.2 - 999.9).</param>
    Task SetBuzzerFeedbackTime(double duration);

    /// <summary>
    /// Считывает текущую конфигурацию устройства и выводит её в консоль.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    Task<SystemDataModel> ReadConfigurationAsync();

    Task<bool> TestReset();
  }
}
