using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.GptLibrary.Data;
using static Core.GptLibrary.Command.ManualCommandManager;
using static Utilities.LoggerUtility;

namespace Core.GptLibrary
{
  static public class DcwMode
  {
    /// <summary>
    /// Устанавливает режим сопротивления изоляции на пробойке.
    /// </summary>
    /// <param name="model">Модель пробойки.</param>
    static public async Task SetModeAsync(Model model)
    {
      LogInformation("Устанавливаем режим СИ на GPT-79904");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} DCW";
      await model.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает напряжение DCW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в кВ).</param>
    public static async Task SetVoltageAsync(Model model, double value)
    {
      LogInformation($"Устанавливаем напряжение DCW: {value:F3} кВ");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_DCW_VOLTAGE)} {value:F3}".Replace(',', '.');
      await model.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает высокий предел тока DCW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в мА).</param>
    public static async Task SetHighCurrentLimitAsync(Model model, double value)
    {
      LogInformation($"Устанавливаем высокий предел тока DCW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_DCW_CHISET)} {value:F3}";
      await model.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает низкий предел тока DCW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в мА).</param>
    public static async Task SetLowCurrentLimitAsync(Model model, double value)
    {
      var query1 = $"{GetCommandSyntax(ManualCommand.MANU_DCW_CLOSET)} {value:F3}".Replace(',', '.');
      LogInformation($"Отправляем команду (Вариант 1): {query1}");
      await model.WriteLineAsync(query1);
      await Task.Delay(100); // Небольшая задержка
    }


    /// <summary>
    /// Устанавливает время теста DCW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в секундах).</param>
    public static async Task SetTestTimeAsync(Model model, double value)
    {
      LogInformation($"Устанавливаем время теста DCW: {value:F1} сек");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_DCW_TTIME)} {value:F1}";
      await model.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает смещение DCW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в мА).</param>
    public static async Task SetOffsetAsync(Model model, double value)
    {
      LogInformation($"Устанавливаем смещение DCW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_DCW_REF)} {value:F3}";
      await model.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает текущее значение тока DCW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в мА).</param>
    public static async Task SetArcCurrentAsync(Model model, double value)
    {
      LogInformation($"Устанавливаем текущее значение тока DCW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_DCW_ARCCURRENT)} {value:F3}";
      await model.WriteLineAsync(query);
    }

    /// <summary>
    /// Считывает текущую конфигурацию DCW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <returns>Объект с текущими параметрами ACW.</returns>
    public static async Task<DcwConfiguration> ReadConfigurationAsync(Model model)
    {
      LogInformation("Считываем конфигурацию DCW...");

      // Чтение напряжения
      var voltageQuery = $"{GetCommandSyntax(ManualCommand.MANU_DCW_VOLTAGE)} ?";
      await model.WriteLineAsync(voltageQuery);
      await Task.Delay(10); // Задержка для обработки ответа устройством
      var voltageResponse = await model.ReadLineAsync();
      double voltage = ParseVoltage(voltageResponse);

      // Чтение высокого предела тока
      var chiQuery = $"{GetCommandSyntax(ManualCommand.MANU_DCW_CHISET)} ?";
      await model.WriteLineAsync(chiQuery);
      var chiResponse = await model.ReadLineAsync();
      double highCurrentLimit = ParseCurrent(chiResponse);

      // Чтение низкого предела тока
      var cloQuery = $"{GetCommandSyntax(ManualCommand.MANU_DCW_CLOSET)} ?";
      await model.WriteLineAsync(cloQuery);
      var cloResponse = await model.ReadLineAsync();
      double lowCurrentLimit = ParseCurrent(cloResponse);

      // Чтение времени теста
      var timeQuery = $"{GetCommandSyntax(ManualCommand.MANU_DCW_TTIME)} ?";
      await model.WriteLineAsync(timeQuery);
      var timeResponse = await model.ReadLineAsync();
      double testTime = ParseTime(timeResponse);

      // Чтение смещения
      var offsetQuery = $"{GetCommandSyntax(ManualCommand.MANU_DCW_REF)} ?";
      await model.WriteLineAsync(offsetQuery);
      var offsetResponse = await model.ReadLineAsync();
      double offset = ParseCurrent(offsetResponse);

      var arcCurrentQuery = $"{GetCommandSyntax(ManualCommand.MANU_DCW_ARCCURRENT)} ?";
      await model.WriteLineAsync(offsetQuery);
      var arcCurrentResponse = await model.ReadLineAsync();
      double arcCurrent = ParseCurrent(offsetResponse);

      // Возвращаем объект конфигурации
      return new DcwConfiguration
      {
        Voltage = voltage,
        HighCurrentLimit = highCurrentLimit,
        LowCurrentLimit = lowCurrentLimit,
        TestTime = testTime,
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
