using NewCore.Base.Function.Breakdown;
using NewCore.Device;
using NewCore.Function.GPT.Data;
using static NewCore.Function.GPT.Command.FunctionCommandManager;
using static NewCore.Function.GPT.Command.ManualCommandManager;
using static Utilities.LoggerUtility;

namespace NewCore.Function.GPT
{
  /// <summary>
  /// Класс для работы с режимом ACW.
  /// </summary>
  public class AcwMode : IAcwModeBreakdown
  {
    public AcwMode(GPT79904 gpt79904) => _gptModel = gpt79904;
    GPT79904 _gptModel { get; set; }

    /// <summary>
    /// Устанавливает режим сопротивления изоляции на пробойке.
    /// </summary>
    /// <param name="model">Модель пробойки.</param>
    public async Task SetModeAsync()
    {
      LogInformation("Устанавливаем режим СИ на GPT-79904");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} ACW";
      await _gptModel.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает напряжение ACW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в кВ).</param>
    public async Task SetVoltageAsync(double value)
    {
      LogInformation($"Устанавливаем напряжение ACW: {value:F3} кВ");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_VOLTAGE)} {value:F3}".Replace(',', '.');
      await _gptModel.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает высокий предел тока ACW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в мА).</param>
    public async Task SetHighCurrentLimitAsync(double value)
    {
      LogInformation($"Устанавливаем высокий предел тока ACW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_CHISET)} {value:F3}";
      await _gptModel.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает низкий предел тока ACW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в мА).</param>
    public async Task SetLowCurrentLimitAsync(double value)
    {
      LogInformation($"Устанавливаем низкий предел тока ACW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_CLOSET)} {value:F3}".Replace(',', '.');
      await _gptModel.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает время теста ACW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в секундах).</param>
    public async Task SetTestTimeAsync(double value)
    {
      LogInformation($"Устанавливаем время теста ACW: {value:F1} сек");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_TTIME)} {value:F1}";
      await _gptModel.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает частоту ACW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="frequency">Частота (50 или 60 Гц).</param>
    public async Task SetFrequencyAsync(int frequency)
    {
      if (frequency != 50 && frequency != 60)
        throw new ArgumentException("Частота должна быть 50 или 60 Гц.");

      LogInformation($"Устанавливаем частоту ACW: {frequency} Гц");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_FREQUENCY)} {frequency}";
      await _gptModel.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает смещение ACW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в мА).</param>
    public async Task SetOffsetAsync(double value)
    {
      LogInformation($"Устанавливаем смещение ACW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_REF)} {value:F3}";
      await _gptModel.WriteLineAsync(query);
    }

    /// <summary>
    /// Устанавливает текущее значение тока ACW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="value">Устанавливаемое значение (в мА).</param>
    public async Task SetArcCurrentAsync(double value)
    {
      LogInformation($"Устанавливаем текущее значение тока ACW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_ARCCURRENT)} {value:F3}";
      await _gptModel.WriteLineAsync(query);
    }

    /// <summary>
    /// Считывает текущую конфигурацию ACW.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <returns>Объект с текущими параметрами ACW.</returns>
    public async Task<AcwConfiguration> ReadConfigurationAsync()
    {
      LogInformation("Считываем конфигурацию ACW...");

      // Чтение напряжения
      var voltageQuery = $"{GetCommandSyntax(ManualCommand.MANU_ACW_VOLTAGE)} ?";
      await _gptModel.WriteLineAsync(voltageQuery);
      await Task.Delay(10); // Задержка для обработки ответа устройством
      var voltageResponse = await _gptModel.ReadLineAsync();
      double voltage = ParseVoltage(voltageResponse);

      // Чтение высокого предела тока
      var chiQuery = $"{GetCommandSyntax(ManualCommand.MANU_ACW_CHISET)} ?";
      await _gptModel.WriteLineAsync(chiQuery);
      var chiResponse = await _gptModel.ReadLineAsync();
      double highCurrentLimit = ParseCurrent(chiResponse);

      // Чтение низкого предела тока
      var cloQuery = $"{GetCommandSyntax(ManualCommand.MANU_ACW_CLOSET)} ?";
      await _gptModel.WriteLineAsync(cloQuery);
      var cloResponse = await _gptModel.ReadLineAsync();
      double lowCurrentLimit = ParseCurrent(cloResponse);

      // Чтение времени теста
      var timeQuery = $"{GetCommandSyntax(ManualCommand.MANU_ACW_TTIME)} ?";
      await _gptModel.WriteLineAsync(timeQuery);
      var timeResponse = await _gptModel.ReadLineAsync();
      double testTime = ParseTime(timeResponse);

      // Чтение частоты
      var frequencyQuery = $"{GetCommandSyntax(ManualCommand.MANU_ACW_FREQUENCY)} ?";
      await _gptModel.WriteLineAsync(frequencyQuery);
      var frequencyResponse = await _gptModel.ReadLineAsync();
      int frequency = ParseFrequency(frequencyResponse);

      // Чтение смещения
      var offsetQuery = $"{GetCommandSyntax(ManualCommand.MANU_ACW_REF)} ?";
      await _gptModel.WriteLineAsync(offsetQuery);
      var offsetResponse = await _gptModel.ReadLineAsync();
      double offset = ParseCurrent(offsetResponse);

      var arcCurrentQuery = $"{GetCommandSyntax(ManualCommand.MANU_ACW_ARCCURRENT)} ?";
      await _gptModel.WriteLineAsync(offsetQuery);
      var arcCurrentResponse = await _gptModel.ReadLineAsync();
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
    /// Парсит значение частоты из строки ответа.
    /// </summary>
    /// <param name="response">Строка ответа.</param>
    /// <returns>Значение частоты (в Гц).</returns>
    private int ParseFrequency(string response)
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
    public async Task<double> MeasureCurrentAsync()
    {
      // TODO : Реализация измерения ACW

      return 00.00;
    }
  }
}
