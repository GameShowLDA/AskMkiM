using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities
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
    Task SetLcdContrastAsync(double value, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Устанавливает яркость дисплея (1 - темный, 2 - яркий).
    /// </summary>
    /// <param name="value">Значение яркости (1 или 2).</param>
    Task SetLcdBrightnessAsync(double value, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Включает/выключает звук успешного теста.
    /// </summary>
    /// <param name="state">Состояние (ON или OFF).</param>
    Task SetBuzzerPrimarySound(bool state, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Включает/выключает звук ошибочного теста.
    /// </summary>
    /// <param name="state">Состояние (ON или OFF).</param>
    Task SetBuzzerFeedbackSound(bool state, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Устанавливает продолжительность звука успешного теста (0.2 - 999.9 секунд).
    /// </summary>
    /// <param name="duration">Длительность сигнала (0.2 - 999.9).</param>
    Task SetBuzzerPrimaryTime(double duration, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Устанавливает продолжительность звука ошибочного теста (0.2 - 999.9 секунд).
    /// </summary>
    /// <param name="duration">Длительность сигнала (0.2 - 999.9).</param>
    Task SetBuzzerFeedbackTime(double duration, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Считывает текущую конфигурацию устройства и выводит её в консоль.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    Task<SystemDataModel> ReadConfigurationAsync();

    Task<bool> TestReset();
  }
}
