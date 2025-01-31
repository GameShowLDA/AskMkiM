using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.GptLibrary.Command;
using Core.GptLibrary.Data.Core.GptLibrary.Models;
using YamlDotNet.Core.Tokens;
using static Core.GptLibrary.Command.SystemCommandManager;

namespace Core.GptLibrary
{
  /// <summary>
  /// Класс для управления системными настройками устройства.
  /// Все методы являются заглушками и просто выводят команду в консоль.
  /// </summary>
  public static class SystemSettings
  {
    /// <summary>
    /// Устанавливает контрастность дисплея (от 1 до 8).
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Значение контрастности (1-8).</param>
    public static async Task SetLcdContrastAsync(Model model, double value)
    {
      var command = GetCommandSyntax(SystemCommand.LCD_CONTRAST) + $" {value}";
      await model.WriteLineAsync(command);
    }

    /// <summary>
    /// Устанавливает яркость дисплея (1 - темный, 2 - яркий).
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Значение яркости (1 или 2).</param>
    public static async Task SetLcdBrightnessAsync(Model model, double value)
    {
      var command = GetCommandSyntax(SystemCommand.LCD_BRIGHTNESS) + $" {value}";
      await model.WriteLineAsync(command);
    }

    /// <summary>
    /// Включает/выключает звук успешного теста.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="state">Состояние (ON или OFF).</param>
    public static async Task SetBuzzerPrimarySound(Model model, bool state)
    {
      var value = state ? "ON" : "OFF";
      var command = GetCommandSyntax(SystemCommand.BUZZER_PSOUND) + $" {value}";
      await model.WriteLineAsync(command);
    }

    /// <summary>
    /// Включает/выключает звук ошибочного теста.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="state">Состояние (ON или OFF).</param>
    public static async Task SetBuzzerFeedbackSound(Model model, bool state)
    {
      var value = state ? "ON" : "OFF";
      var command = GetCommandSyntax(SystemCommand.BUZZER_FSOUND) + $" {value}";
      await model.WriteLineAsync(command);
    }

    /// <summary>
    /// Устанавливает продолжительность звука успешного теста (0.2 - 999.9 секунд).
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="duration">Длительность сигнала (0.2 - 999.9).</param>
    public static async Task SetBuzzerPrimaryTime(Model model, double duration)
    {
      var command = GetCommandSyntax(SystemCommand.BUZZER_PTIME) + $" {duration}";
      await model.WriteLineAsync(command);
    }

    /// <summary>
    /// Устанавливает продолжительность звука ошибочного теста (0.2 - 999.9 секунд).
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="duration">Длительность сигнала (0.2 - 999.9).</param>
    public static async Task SetBuzzerFeedbackTime(Model model, double duration)
    {
      var command = GetCommandSyntax(SystemCommand.BUZZER_FTIME) + $" {duration}";
      await model.WriteLineAsync(command);
    }

    /// <summary>
    /// Считывает текущую конфигурацию устройства и выводит её в консоль.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    public static async Task<SystemDataModel> ReadConfigurationAsync(Model model)
    {
      var systemData = new SystemDataModel();

      try
      {
        Console.WriteLine("=== Чтение конфигурации устройства ===");

        // Запрос контраста дисплея
        string contrastCommand = GetCommandSyntax(SystemCommand.LCD_CONTRAST) + "?";
        await model.WriteLineAsync(contrastCommand);
        string contrastResponse = await model.ReadLineAsync();
        systemData.LcdContrast = int.Parse(contrastResponse);
        Console.WriteLine($"Контраст дисплея: {systemData.LcdContrast}");

        // Запрос яркости дисплея
        string brightnessCommand = GetCommandSyntax(SystemCommand.LCD_BRIGHTNESS) + "?";
        await model.WriteLineAsync(brightnessCommand);
        string brightnessResponse = await model.ReadLineAsync();
        systemData.LcdBrightness = int.Parse(brightnessResponse);
        Console.WriteLine($"Яркость дисплея: {systemData.LcdBrightness}");

        // Запрос состояния звука успешного теста
        string buzzerPSoundCommand = GetCommandSyntax(SystemCommand.BUZZER_PSOUND) + "?";
        await model.WriteLineAsync(buzzerPSoundCommand);
        string buzzerPSoundResponse = await model.ReadLineAsync();
        systemData.BuzzerPrimarySound = buzzerPSoundResponse.Trim().ToUpper() == "ON";
        Console.WriteLine($"Звук успешного теста: {systemData.BuzzerPrimarySound}");

        // Запрос состояния звука ошибочного теста
        string buzzerFSoundCommand = GetCommandSyntax(SystemCommand.BUZZER_FSOUND) + "?";
        await model.WriteLineAsync(buzzerFSoundCommand);
        string buzzerFSoundResponse = await model.ReadLineAsync();
        systemData.BuzzerFeedbackSound = buzzerFSoundResponse.Trim().ToUpper() == "ON";
        Console.WriteLine($"Звук ошибочного теста: {systemData.BuzzerFeedbackSound}");

        // Запрос продолжительности звука успешного теста
        string buzzerPTimeCommand = GetCommandSyntax(SystemCommand.BUZZER_PTIME) + "?";
        await model.WriteLineAsync(buzzerPTimeCommand);
        string buzzerPTimeResponse = await model.ReadLineAsync();
        systemData.BuzzerPrimaryTime = double.Parse(buzzerPTimeResponse);
        Console.WriteLine($"Продолжительность звука успешного теста: {systemData.BuzzerPrimaryTime}");

        // Запрос продолжительности звука ошибочного теста
        string buzzerFTimeCommand = GetCommandSyntax(SystemCommand.BUZZER_FTIME) + "?";
        await model.WriteLineAsync(buzzerFTimeCommand);
        string buzzerFTimeResponse = await model.ReadLineAsync();
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
