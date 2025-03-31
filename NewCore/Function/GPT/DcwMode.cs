using NewCore.Device;
using NewCore.Function.GPT.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NewCore.Function.GPT.Command.FunctionCommandManager;
using static NewCore.Function.GPT.Command.ManualCommandManager;
using static Utilities.LoggerUtility;

namespace NewCore.Function.GPT
{
  public class DcwMode
  {
    public DcwMode(GPT79904 gpt79904) => _gptModel = gpt79904;
    GPT79904 _gptModel { get; set; }

    /// <summary>
    /// Устанавливает режим сопротивления изоляции на пробойке.
    /// </summary>
    /// <param name="model">Модель пробойки.</param>
    public async Task SetModeAsync()
    {
      LogInformation("Устанавливаем режим СИ на GPT-79904");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} DCW";
      await _gptModel.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает напряжение DCW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в кВ).</param>
    public async Task SetVoltageAsync( double value)
    {
      LogInformation($"Устанавливаем напряжение DCW: {value:F3} кВ");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_DCW_VOLTAGE)} {value:F3}".Replace(',', '.');
      await _gptModel.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает высокий предел тока DCW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в мА).</param>
    public async Task SetHighCurrentLimitAsync( double value)
    {
      LogInformation($"Устанавливаем высокий предел тока DCW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_DCW_CHISET)} {value:F3}";
      await _gptModel.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает низкий предел тока DCW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в мА).</param>
    public async Task SetLowCurrentLimitAsync( double value)
    {
      var query1 = $"{GetCommandSyntax(ManualCommand.MANU_DCW_CLOSET)} {value:F3}".Replace(',', '.');
      LogInformation($"Отправляем команду (Вариант 1): {query1}");
      await _gptModel.WriteLineAsync(query1);
      await Task.Delay(100); // Небольшая задержка
    }


    /// <summary>
    /// Устанавливает время теста DCW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в секундах).</param>
    public async Task SetTestTimeAsync( double value)
    {
      LogInformation($"Устанавливаем время теста DCW: {value:F1} сек");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_DCW_TTIME)} {value:F1}";
      await _gptModel.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает смещение DCW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в мА).</param>
    public async Task SetOffsetAsync( double value)
    {
      LogInformation($"Устанавливаем смещение DCW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_DCW_REF)} {value:F3}";
      await _gptModel.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает текущее значение тока DCW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в мА).</param>
    public async Task SetArcCurrentAsync( double value)
    {
      LogInformation($"Устанавливаем текущее значение тока DCW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_DCW_ARCCURRENT)} {value:F3}";
      await _gptModel.WriteLineAsync(query);
    }

    /// <summary>
    /// Считывает текущую конфигурацию DCW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <returns>Объект с текущими параметрами ACW.</returns>
    public async Task<DcwConfiguration> ReadConfigurationAsync()
    {
      LogInformation("Считываем конфигурацию DCW...");

      // Чтение напряжения
      var voltageQuery = $"{GetCommandSyntax(ManualCommand.MANU_DCW_VOLTAGE)} ?";
      await _gptModel.WriteLineAsync(voltageQuery);
      await Task.Delay(10); // Задержка для обработки ответа устройством
      var voltageResponse = await _gptModel.ReadLineAsync();
      double voltage = ParseVoltage(voltageResponse);

      // Чтение высокого предела тока
      var chiQuery = $"{GetCommandSyntax(ManualCommand.MANU_DCW_CHISET)} ?";
      await _gptModel.WriteLineAsync(chiQuery);
      var chiResponse = await _gptModel.ReadLineAsync();
      double highCurrentLimit = ParseCurrent(chiResponse);

      // Чтение низкого предела тока
      var cloQuery = $"{GetCommandSyntax(ManualCommand.MANU_DCW_CLOSET)} ?";
      await _gptModel.WriteLineAsync(cloQuery);
      var cloResponse = await _gptModel.ReadLineAsync();
      double lowCurrentLimit = ParseCurrent(cloResponse);

      // Чтение времени теста
      var timeQuery = $"{GetCommandSyntax(ManualCommand.MANU_DCW_TTIME)} ?";
      await _gptModel.WriteLineAsync(timeQuery);
      var timeResponse = await _gptModel.ReadLineAsync();
      double testTime = ParseTime(timeResponse);

      // Чтение смещения
      var offsetQuery = $"{GetCommandSyntax(ManualCommand.MANU_DCW_REF)} ?";
      await _gptModel.WriteLineAsync(offsetQuery);
      var offsetResponse = await _gptModel.ReadLineAsync();
      double offset = ParseCurrent(offsetResponse);

      var arcCurrentQuery = $"{GetCommandSyntax(ManualCommand.MANU_DCW_ARCCURRENT)} ?";
      await _gptModel.WriteLineAsync(offsetQuery);
      var arcCurrentResponse = await _gptModel.ReadLineAsync();
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
    private double ParseVoltage(string response)
    {
      var value = response.Replace("kV", "").Trim().Replace(".", ",");
      return double.Parse(value);
    }

    /// <summary>
    /// Парсит значение тока из строки ответа.
    /// </summary>
    /// <param name="response">Строка ответа.</param>
    /// <returns>Значение тока (в мА).</returns>
    private double ParseCurrent(string response)
    {
      var value = response.Replace("mA", "").Trim().Replace(".", ",");
      return double.Parse(value);
    }

    /// <summary>
    /// Парсит значение времени из строки ответа.
    /// </summary>
    /// <param name="response">Строка ответа.</param>
    /// <returns>Значение времени (в секундах).</returns>
    private double ParseTime(string response)
    {
      var value = response.Replace("S", "").Trim().Replace(".", ",");
      return double.Parse(value);
    }

    /// <summary>
    /// Запускает тест ACW и возвращает результат.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <returns>Результат теста (в мА).</returns>
    public async Task<double> MeasureCurrentAsync()
    {
      // TODO : Реализация измерения DCW

      return 00.00;
    }
  }
}
