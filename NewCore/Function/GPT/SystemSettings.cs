using NewCore.Device;
using NewCore.Function.GPT.Data;
using static NewCore.Function.GPT.Command.FunctionCommandManager;
using static NewCore.Function.GPT.Command.ManualCommandManager;
using static NewCore.Function.GPT.Command.SystemCommandManager;

namespace NewCore.Function.GPT
{
  /// <summary>
  /// Класс для управления системными настройками устройства.
  /// Все методы являются заглушками и просто выводят команду в консоль.
  /// </summary>
  public class SystemSettings
  {

    public SystemSettings(GPT79904 gpt79904) => _gptModel = gpt79904;
    GPT79904 _gptModel { get; set; }

    /// <summary>
    /// Устанавливает контрастность дисплея (от 1 до 8).
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Значение контрастности (1-8).</param>
    public async Task SetLcdContrastAsync(double value)
    {
      var command = GetCommandSyntax(SystemCommand.LCD_CONTRAST) + $" {value}";
      await _gptModel.WriteLineAsync(command);
    }

    /// <summary>
    /// Устанавливает яркость дисплея (1 - темный, 2 - яркий).
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Значение яркости (1 или 2).</param>
    public async Task SetLcdBrightnessAsync(double value)
    {
      var command = GetCommandSyntax(SystemCommand.LCD_BRIGHTNESS) + $" {value}";
      await _gptModel.WriteLineAsync(command);
    }

    /// <summary>
    /// Включает/выключает звук успешного теста.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="state">Состояние (ON или OFF).</param>
    public async Task SetBuzzerPrimarySound(bool state)
    {
      var value = state ? "ON" : "OFF";
      var command = GetCommandSyntax(SystemCommand.BUZZER_PSOUND) + $" {value}";
      await _gptModel.WriteLineAsync(command);
    }

    /// <summary>
    /// Включает/выключает звук ошибочного теста.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="state">Состояние (ON или OFF).</param>
    public async Task SetBuzzerFeedbackSound(bool state)
    {
      var value = state ? "ON" : "OFF";
      var command = GetCommandSyntax(SystemCommand.BUZZER_FSOUND) + $" {value}";
      await _gptModel.WriteLineAsync(command);
    }

    /// <summary>
    /// Устанавливает продолжительность звука успешного теста (0.2 - 999.9 секунд).
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="duration">Длительность сигнала (0.2 - 999.9).</param>
    public async Task SetBuzzerPrimaryTime(double duration)
    {
      var command = GetCommandSyntax(SystemCommand.BUZZER_PTIME) + $" {duration}";
      await _gptModel.WriteLineAsync(command);
    }

    /// <summary>
    /// Устанавливает продолжительность звука ошибочного теста (0.2 - 999.9 секунд).
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="duration">Длительность сигнала (0.2 - 999.9).</param>
    public async Task SetBuzzerFeedbackTime(double duration)
    {
      var command = GetCommandSyntax(SystemCommand.BUZZER_FTIME) + $" {duration}";
      await _gptModel.WriteLineAsync(command);
    }

    /// <summary>
    /// Считывает текущую конфигурацию устройства и выводит её в консоль.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    public async Task<SystemDataModel> ReadConfigurationAsync()
    {
      var systemData = new SystemDataModel();

      try
      {
        Console.WriteLine("=== Чтение конфигурации устройства ===");

        // Запрос контраста дисплея
        string contrastCommand = GetCommandSyntax(SystemCommand.LCD_CONTRAST) + "?";
        await _gptModel.WriteLineAsync(contrastCommand);
        string contrastResponse = await _gptModel.ReadLineAsync();
        systemData.LcdContrast = int.Parse(contrastResponse);
        Console.WriteLine($"Контраст дисплея: {systemData.LcdContrast}");

        // Запрос яркости дисплея
        string brightnessCommand = GetCommandSyntax(SystemCommand.LCD_BRIGHTNESS) + "?";
        await _gptModel.WriteLineAsync(brightnessCommand);
        string brightnessResponse = await _gptModel.ReadLineAsync();
        systemData.LcdBrightness = int.Parse(brightnessResponse);
        Console.WriteLine($"Яркость дисплея: {systemData.LcdBrightness}");

        // Запрос состояния звука успешного теста
        string buzzerPSoundCommand = GetCommandSyntax(SystemCommand.BUZZER_PSOUND) + "?";
        await _gptModel.WriteLineAsync(buzzerPSoundCommand);
        string buzzerPSoundResponse = await _gptModel.ReadLineAsync();
        systemData.BuzzerPrimarySound = buzzerPSoundResponse.Trim().ToUpper() == "ON";
        Console.WriteLine($"Звук успешного теста: {systemData.BuzzerPrimarySound}");

        // Запрос состояния звука ошибочного теста
        string buzzerFSoundCommand = GetCommandSyntax(SystemCommand.BUZZER_FSOUND) + "?";
        await _gptModel.WriteLineAsync(buzzerFSoundCommand);
        string buzzerFSoundResponse = await _gptModel.ReadLineAsync();
        systemData.BuzzerFeedbackSound = buzzerFSoundResponse.Trim().ToUpper() == "ON";
        Console.WriteLine($"Звук ошибочного теста: {systemData.BuzzerFeedbackSound}");

        // Запрос продолжительности звука успешного теста
        string buzzerPTimeCommand = GetCommandSyntax(SystemCommand.BUZZER_PTIME) + "?";
        await _gptModel.WriteLineAsync(buzzerPTimeCommand);
        string buzzerPTimeResponse = await _gptModel.ReadLineAsync();
        systemData.BuzzerPrimaryTime = double.Parse(buzzerPTimeResponse);
        Console.WriteLine($"Продолжительность звука успешного теста: {systemData.BuzzerPrimaryTime}");

        // Запрос продолжительности звука ошибочного теста
        string buzzerFTimeCommand = GetCommandSyntax(SystemCommand.BUZZER_FTIME) + "?";
        await _gptModel.WriteLineAsync(buzzerFTimeCommand);
        string buzzerFTimeResponse = await _gptModel.ReadLineAsync();
        systemData.BuzzerFeedbackTime = double.Parse(buzzerFTimeResponse);
        Console.WriteLine($"Продолжительность звука ошибочного теста: {systemData.BuzzerFeedbackTime}");

        Console.WriteLine("===============================");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка при чтении конфигурации: {ex.Message}");
      }

      return systemData;
    }
  }
}
