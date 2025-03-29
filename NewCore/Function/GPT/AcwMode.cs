using System.Globalization;
using System.Text.RegularExpressions;
using NewCore.Base.Function.Breakdown;
using NewCore.Device;
using NewCore.Function.GPT.Command;
using NewCore.Function.GPT.Data;
using static NewCore.Function.GPT.Command.FunctionCommandManager;
using static NewCore.Function.GPT.Command.ManualCommandManager;
using static Utilities.LoggerUtility;

namespace NewCore.Function.GPT
{
  /// <summary>
  /// Класс для работы с режимом ACW (переменный ток высокого напряжения).
  /// </summary>
  public class AcwMode : IAcwModeBreakdown
  {
    /// <summary>
    /// Создает новый экземпляр класса <see cref="AcwMode"/>.
    /// </summary>
    /// <param name="gpt79904">Объект устройства GPT-79904.</param>
    public AcwMode(GPT79904 gpt79904) => _gptModel = gpt79904;

    /// <summary>
    /// Модель устройства GPT-79904.
    /// </summary>
    private GPT79904 _gptModel { get; set; }

    static private int delayBeforeCall = 100;

    /// <summary>
    /// Устанавливает режим ACW (переменного высокого напряжения).
    /// </summary>
    public async Task SetModeAsync()
    {
      LogInformation("Устанавливаем режим ACW на GPT-79904");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} ACW";
      await _gptModel.DeviceProtocol.QueryAsync(query);
    }

    /// <summary>
    /// Устанавливает напряжение ACW.
    /// </summary>
    /// <param name="value">Напряжение в кВ.</param>
    public async Task SetVoltageAsync(double value)
    {
      LogInformation($"Устанавливаем напряжение ACW: {value:F3} кВ");
      value /= 1000;
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_VOLTAGE)} {value:F3}".Replace(',', '.');
      await _gptModel.DeviceProtocol.QueryAsync(query);
    }

    /// <summary>
    /// Устанавливает верхний предел тока ACW.
    /// </summary>
    /// <param name="value">Ток в мА.</param>
    public async Task SetHighCurrentLimitAsync(double value)
    {
      LogInformation($"Устанавливаем верхний предел тока ACW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_CHISET)} {value:F3}";
      await _gptModel.DeviceProtocol.QueryAsync(query);
    }

    /// <summary>
    /// Устанавливает нижний предел тока ACW.
    /// </summary>
    /// <param name="value">Ток в мА.</param>
    public async Task SetLowCurrentLimitAsync(double value)
    {
      LogInformation($"Устанавливаем нижний предел тока ACW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_CLOSET)} {value:F3}".Replace(',', '.');
      await _gptModel.DeviceProtocol.QueryAsync(query);
    }

    /// <summary>
    /// Устанавливает время теста ACW.
    /// </summary>
    /// <param name="value">Время в секундах.</param>
    public async Task SetTestTimeAsync(double value)
    {
      LogInformation($"Устанавливаем время теста ACW: {value:F1} сек");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_TTIME)} {value:F1}";
      await _gptModel.DeviceProtocol.QueryAsync(query);
    }

    /// <summary>
    /// Устанавливает время нарастания напряжения (Ramp Time) для текущего теста.
    /// </summary>
    /// <param name="value">Значение времени нарастания в секундах (0.1 – 999.9).</param>
    public async Task SetRampTimeAsync(double value)
    {
      var rampTime = Convert.ToInt32(value);
      LogInformation($"Устанавливаем время нарастания напряжения: {value:F1} сек");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_RTIME)} {value:F1}".Replace(',', '.');
      await _gptModel.DeviceProtocol.QueryAsync(query);
    }

    /// <summary>
    /// Устанавливает частоту ACW.
    /// </summary>
    /// <param name="frequency">Частота (50 или 60 Гц).</param>
    /// <exception cref="ArgumentException">Выбрасывается, если частота не равна 50 или 60 Гц.</exception>
    public async Task SetFrequencyAsync(int frequency)
    {
      if (frequency != 50 && frequency != 60)
      {
        throw new ArgumentException("Частота должна быть 50 или 60 Гц.");
      }

      LogInformation($"Устанавливаем частоту ACW: {frequency} Гц");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_FREQUENCY)} {frequency}";
      await _gptModel.DeviceProtocol.QueryAsync(query);
    }

    /// <summary>
    /// Устанавливает смещение ACW.
    /// </summary>
    /// <param name="value">Смещение в мА.</param>
    public async Task SetOffsetAsync(double value)
    {
      LogInformation($"Устанавливаем смещение ACW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_REF)} {value:F3}";
      await _gptModel.DeviceProtocol.QueryAsync(query);
    }

    /// <summary>
    /// Устанавливает предельное значение тока дугового пробоя ACW.
    /// </summary>
    /// <param name="value">Ток в мА.</param>
    public async Task SetArcCurrentAsync(double value)
    {
      LogInformation($"Устанавливаем предельное значение дугового тока ACW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_ARCCURRENT)} {value:F3}";
      await _gptModel.DeviceProtocol.QueryAsync(query);
    }

    /// <summary>
    /// Считывает текущую конфигурацию ACW.
    /// </summary>
    /// <returns>Объект <see cref="AcwConfiguration"/> с текущими параметрами.</returns>
    public async Task<AcwConfiguration> ReadConfigurationAsync()
    {
      LogInformation("Считываем конфигурацию ACW...");

      // Чтение параметров
      double voltage = await ReadDoubleParameterAsync(ManualCommand.MANU_ACW_VOLTAGE, "kV");
      double highCurrentLimit = await ReadDoubleParameterAsync(ManualCommand.MANU_ACW_CHISET, "mA");
      double lowCurrentLimit = await ReadDoubleParameterAsync(ManualCommand.MANU_ACW_CLOSET, "mA");
      double testTime = await ReadDoubleParameterAsync(ManualCommand.MANU_ACW_TTIME, "S");
      int frequency = await ReadIntParameterAsync(ManualCommand.MANU_ACW_FREQUENCY, "Hz");
      double offset = await ReadDoubleParameterAsync(ManualCommand.MANU_ACW_REF, "mA");
      double arcCurrent = await ReadDoubleParameterAsync(ManualCommand.MANU_ACW_ARCCURRENT, "mA");

      return new AcwConfiguration
      {
        Voltage = voltage,
        HighCurrentLimit = highCurrentLimit,
        LowCurrentLimit = lowCurrentLimit,
        TestTime = testTime,
        Frequency = frequency,
        Offset = offset,
        ArcCurrent = arcCurrent,
      };
    }

    /// <summary>
    /// Запускает тест ACW и возвращает измеренный ток.
    /// </summary>
    /// <returns>Измеренное значение тока (в мА).</returns>
    public async Task<double> MeasureCurrentAsync()
    {
      LogInformation("Запуск измерений режима ПИ ACW");
      var query = $"{FunctionCommandManager.GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} ON";
      var timeDelay = Convert.ToInt32(await GetRampTimeAsync() + await GetTestTimeAsync());

      await _gptModel.DeviceProtocol.QueryAsync(query, responseDelay: timeDelay * 1000, delayBeforeCall: delayBeforeCall);
      query = $"{FunctionCommandManager.GetCommandSyntax(FunctionCommand.MEASURE)} ?";
      var answerDevice = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 500, delayBeforeCall: delayBeforeCall);

      var result = answerDevice.Split(',');
      var measureResulte = result[3];

      LogInformation($"Результат измерения режима ПИ(ACW): {measureResulte}");

      Match match = Regex.Match(measureResulte, @"\d+(\.\d+)?");

      if (match.Success)
      {
        return double.Parse(match.Value, CultureInfo.InvariantCulture);
      }

      throw new FormatException("Число не найдено в строке.");
    }

    /// <summary>
    /// Считывает числовой параметр из устройства.
    /// </summary>
    /// <param name="command">Команда запроса.</param>
    /// <param name="unit">Единица измерения.</param>
    /// <returns>Извлеченное значение.</returns>
    private async Task<double> ReadDoubleParameterAsync(ManualCommand command, string unit)
    {
      var query = $"{GetCommandSyntax(command)} ?";
      var response = await _gptModel.DeviceProtocol.QueryAsync(query, 100);
      return double.Parse(response.Replace(unit, "").Trim().Replace(".", ","));
    }

    /// <summary>
    /// Считывает целочисленный параметр из устройства.
    /// </summary>
    /// <param name="command">Команда запроса.</param>
    /// <param name="unit">Единица измерения.</param>
    /// <returns>Извлеченное значение.</returns>
    private async Task<int> ReadIntParameterAsync(ManualCommand command, string unit)
    {
      var query = $"{GetCommandSyntax(command)} ?";
      var response = await _gptModel.DeviceProtocol.QueryAsync(query, 100);
      return int.Parse(response.Replace(unit, "").Trim());
    }

    /// <summary>
    /// Получает текущее время нарастания напряжения (Ramp Time) для текущего теста.
    /// </summary>
    /// <returns>Значение времени нарастания в секундах.</returns>
    public async Task<double> GetRampTimeAsync()
    {
      var query = GetCommandSyntax(ManualCommand.MANU_RTIME) + "?";
      LogInformation("Запрашиваем текущее время нарастания напряжения (Ramp Time)...");

      string response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
      LogDebug($"Ответ на запрос Ramp Time: \"{response}\"");

      // Ищем число с точкой: 005.0, 12.3 и т.д.
      var match = Regex.Match(response, @"\d+(\.\d+)?");
      if (match.Success && double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rampTime))
      {
        return rampTime;
      }

      LogWarning("Не удалось разобрать значение Ramp Time. Возвращаем 0.0.");
      return 0.0;
    }

    /// <summary>
    /// Получает текущее время теста DCW.
    /// </summary>
    /// <returns>Значение времени в секундах.</returns>
    public async Task<double> GetTestTimeAsync()
    {
      var query = GetCommandSyntax(ManualCommand.MANU_ACW_TTIME) + "?";
      LogInformation("Запрашиваем время теста DCW...");

      string response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
      LogDebug($"Ответ на запрос DCW Test Time: \"{response}\"");

      var match = Regex.Match(response, @"\d+(\.\d+)?");
      if (match.Success && double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var testTime))
      {
        return testTime;
      }

      LogWarning("Не удалось разобрать значение времени теста DCW. Возвращаем 0.0.");
      return 0.0;
    }
  }
}
