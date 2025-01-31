using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.GptLibrary.Data;
using static Core.GptLibrary.Command.ManualCommandManager;
using static Utilities.LoggerUtility;

namespace Core.GptLibrary
{
  /// <summary>
  /// Класс для работы с режимом ACW.
  /// </summary>
  public static class AcwMode
  {
    /// <summary>
    /// Устанавливает режим сопротивления изоляции на пробойке.
    /// </summary>
    /// <param name="model">Модель пробойки.</param>
    static public async Task SetModeAsync(GptLibrary.Model model)
    {
      LogInformation("Устанавливаем режим СИ на GPT-79904");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} ACW";
      await model.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает напряжение ACW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в кВ).</param>
    public static async Task SetVoltageAsync(Model model, double value)
    {
      LogInformation($"Устанавливаем напряжение ACW: {value:F3} кВ");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_VOLTAGE)} {value:F3}".Replace(',', '.');
      await model.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает высокий предел тока ACW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в мА).</param>
    public static async Task SetHighCurrentLimitAsync(Model model, double value)
    {
      LogInformation($"Устанавливаем высокий предел тока ACW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_CHISET)} {value:F3}";
      await model.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает низкий предел тока ACW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в мА).</param>
    public static async Task SetLowCurrentLimitAsync(Model model, double value)
    {
      LogInformation($"Устанавливаем низкий предел тока ACW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_CLOSET)} {value:F3}".Replace(',', '.');
      await model.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает время теста ACW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в секундах).</param>
    public static async Task SetTestTimeAsync(Model model, double value)
    {
      LogInformation($"Устанавливаем время теста ACW: {value:F1} сек");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_TTIME)} {value:F1}";
      await model.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает частоту ACW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="frequency">Частота (50 или 60 Гц).</param>
    public static async Task SetFrequencyAsync(Model model, int frequency)
    {
      if (frequency != 50 && frequency != 60)
        throw new ArgumentException("Частота должна быть 50 или 60 Гц.");

      LogInformation($"Устанавливаем частоту ACW: {frequency} Гц");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_FREQUENCY)} {frequency}";
      await model.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает смещение ACW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в мА).</param>
    public static async Task SetOffsetAsync(Model model, double value)
    {
      LogInformation($"Устанавливаем смещение ACW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_REF)} {value:F3}";
      await model.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает текущее значение тока ACW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в мА).</param>
    public static async Task SetArcCurrentAsync(Model model, double value)
    {
      LogInformation($"Устанавливаем текущее значение тока ACW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_ARCCURRENT)} {value:F3}";
      await model.WriteLineAsync(query);
    }

    /// <summary>
    /// Считывает текущую конфигурацию ACW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <returns>Объект с текущими параметрами ACW.</returns>
    public static async Task<AcwConfiguration> ReadConfigurationAsync(Model model)
    {
      LogInformation("Считываем конфигурацию ACW...");

      // Чтение напряжения
      var voltageQuery = $"{GetCommandSyntax(ManualCommand.MANU_ACW_VOLTAGE)} ?";
      await model.WriteLineAsync(voltageQuery);
      await Task.Delay(10); // Задержка для обработки ответа устройством
      var voltageResponse = await model.ReadLineAsync();
      double voltage = ParseVoltage(voltageResponse);

      // Чтение высокого предела тока
      var chiQuery = $"{GetCommandSyntax(ManualCommand.MANU_ACW_CHISET)} ?";
      await model.WriteLineAsync(chiQuery);
      var chiResponse = await model.ReadLineAsync();
      double highCurrentLimit = ParseCurrent(chiResponse);

      // Чтение низкого предела тока
      var cloQuery = $"{GetCommandSyntax(ManualCommand.MANU_ACW_CLOSET)} ?";
      await model.WriteLineAsync(cloQuery);
      var cloResponse = await model.ReadLineAsync();
      double lowCurrentLimit = ParseCurrent(cloResponse);

      // Чтение времени теста
      var timeQuery = $"{GetCommandSyntax(ManualCommand.MANU_ACW_TTIME)} ?";
      await model.WriteLineAsync(timeQuery);
      var timeResponse = await model.ReadLineAsync();
      double testTime = ParseTime(timeResponse);

      // Чтение частоты
      var frequencyQuery = $"{GetCommandSyntax(ManualCommand.MANU_ACW_FREQUENCY)} ?";
      await model.WriteLineAsync(frequencyQuery);
      var frequencyResponse = await model.ReadLineAsync();
      int frequency = ParseFrequency(frequencyResponse);

      // Чтение смещения
      var offsetQuery = $"{GetCommandSyntax(ManualCommand.MANU_ACW_REF)} ?";
      await model.WriteLineAsync(offsetQuery);
      var offsetResponse = await model.ReadLineAsync();
      double offset = ParseCurrent(offsetResponse);

      var arcCurrentQuery = $"{GetCommandSyntax(ManualCommand.MANU_ACW_ARCCURRENT)} ?";
      await model.WriteLineAsync(offsetQuery);
      var arcCurrentResponse = await model.ReadLineAsync();
      double arcCurrent = ParseCurrent(offsetResponse);

      // Возвращаем объект конфигурации
      return new AcwConfiguration
      {
        Voltage = voltage,
        HighCurrentLimit = highCurrentLimit,
        LowCurrentLimit = lowCurrentLimit,
        TestTime = testTime,
        Frequency = frequency,
        Offset = offset
      };
    }

    /// <summary>
    /// Парсит значение напряжения из строки ответа.
    /// </summary>
    /// <param name="response">Строка ответа.</param>
    /// <returns>Значение напряжения (в кВ).</returns>
    private static double ParseVoltage(string response)
    {
      var value = response.Replace("kV", "").Trim().Replace(".", ",");
      return double.Parse(value);
    }

    /// <summary>
    /// Парсит значение тока из строки ответа.
    /// </summary>
    /// <param name="response">Строка ответа.</param>
    /// <returns>Значение тока (в мА).</returns>
    private static double ParseCurrent(string response)
    {
      var value = response.Replace("mA", "").Trim().Replace(".", ",");
      return double.Parse(value);
    }

    /// <summary>
    /// Парсит значение времени из строки ответа.
    /// </summary>
    /// <param name="response">Строка ответа.</param>
    /// <returns>Значение времени (в секундах).</returns>
    private static double ParseTime(string response)
    {
      var value = response.Replace("S", "").Trim().Replace(".", ",");
      return double.Parse(value);
    }

    /// <summary>
    /// Парсит значение частоты из строки ответа.
    /// </summary>
    /// <param name="response">Строка ответа.</param>
    /// <returns>Значение частоты (в Гц).</returns>
    private static int ParseFrequency(string response)
    {
      // Предполагается, что ответ имеет формат "MANU:ACW:FREQUENCY XX"
      var value = response.Replace("Hz", "").Trim().Replace(".", ",");
      return int.Parse(value);
    }

    /// <summary>
    /// Запускает тест ACW и возвращает результат.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <returns>Результат теста (в мА).</returns>
    public static async Task<double> MeasureCurrentAsync(Model model)
    {
      //LogInformation("Запускаем тест ACW...");
      //var result = await model.QueryDoubleAsync("MANU:ACW:MEASURE?");
      //return result;
      return 00.00;
    }
  }
}
