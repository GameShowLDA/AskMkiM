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
  /// Класс для управления режимом DCW (Direct Current Withstand).
  /// </summary>
  public class DcwMode : IDcwModeBreakdown
  {
    /// <summary>
    /// Экземпляр устройства GPT79904.
    /// </summary>
    private GPT79904 _gptModel { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DcwMode"/>.
    /// </summary>
    /// <param name="gpt79904">Объект <see cref="GPT79904"/> для управления устройством.</param>
    public DcwMode(GPT79904 gpt79904) => _gptModel = gpt79904;

    static private int delayBeforeCall = 100;

    /// <summary>
    /// Устанавливает режим DCW на устройстве.
    /// </summary>
    public async Task SetModeAsync()
    {
      LogInformation("Устанавливаем режим DCW на GPT-79904");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} DCW";
      await _gptModel.DeviceProtocol.QueryAsync(query);
    }

    /// <summary>
    /// Устанавливает напряжение DCW.
    /// </summary>
    /// <param name="value">Значение напряжения (в В).</param>
    public async Task SetVoltageAsync(double value)
    {
      LogInformation($"Устанавливаем напряжение DCW: {value:F3} кВ");
      value /= 1000;
      var query = $"{GetCommandSyntax(ManualCommand.MANU_DCW_VOLTAGE)} {value:F3}".Replace(',', '.');
      await _gptModel.DeviceProtocol.QueryAsync(query);
    }

    /// <summary>
    /// Устанавливает высокий предел тока DCW.
    /// </summary>
    /// <param name="value">Значение предела (в мА).</param>
    public async Task SetHighCurrentLimitAsync(double value)
    {
      LogInformation($"Устанавливаем высокий предел тока DCW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_DCW_CHISET)} {value:F3}";
      await _gptModel.DeviceProtocol.QueryAsync(query);
    }

    /// <summary>
    /// Устанавливает низкий предел тока DCW.
    /// </summary>
    /// <param name="value">Значение предела (в мА).</param>
    public async Task SetLowCurrentLimitAsync(double value)
    {
      var query1 = $"{GetCommandSyntax(ManualCommand.MANU_DCW_CLOSET)} {value:F3}".Replace(',', '.');
      LogInformation($"Отправляем команду (Вариант 1): {query1}");
      await _gptModel.DeviceProtocol.QueryAsync(query1);
    }

    /// <summary>
    /// Устанавливает время теста DCW.
    /// </summary>
    /// <param name="value">Значение времени (в секундах).</param>
    public async Task SetTestTimeAsync(double value)
    {
      LogInformation($"Устанавливаем время теста DCW: {value:F1} сек");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_DCW_TTIME)} {value:F1}";
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
    /// Устанавливает смещение DCW.
    /// </summary>
    /// <param name="value">Значение смещения (в мА).</param>
    public async Task SetOffsetAsync(double value)
    {
      LogInformation($"Устанавливаем смещение DCW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_DCW_REF)} {value:F3}";
      await _gptModel.DeviceProtocol.QueryAsync(query);
    }

    /// <summary>
    /// Устанавливает значение тока дуги DCW.
    /// </summary>
    /// <param name="value">Значение тока дуги (в мА).</param>
    public async Task SetArcCurrentAsync(double value)
    {
      LogInformation($"Устанавливаем текущее значение тока DCW: {value:F3} мА");
      var query = $"{GetCommandSyntax(ManualCommand.MANU_DCW_ARCCURRENT)} {value:F3}";
      await _gptModel.DeviceProtocol.QueryAsync(query);
    }

    /// <summary>
    /// Считывает текущую конфигурацию DCW.
    /// </summary>
    /// <returns>Объект <see cref="DcwConfiguration"/> с текущими параметрами.</returns>
    public async Task<DcwConfiguration> ReadConfigurationAsync()
    {
      LogInformation("Считываем конфигурацию DCW...");

      var voltage = await ReadDoubleAsync(ManualCommand.MANU_DCW_VOLTAGE);
      var highCurrentLimit = await ReadDoubleAsync(ManualCommand.MANU_DCW_CHISET);
      var lowCurrentLimit = await ReadDoubleAsync(ManualCommand.MANU_DCW_CLOSET);
      var testTime = await ReadDoubleAsync(ManualCommand.MANU_DCW_TTIME);
      var offset = await ReadDoubleAsync(ManualCommand.MANU_DCW_REF);
      var arcCurrent = await ReadDoubleAsync(ManualCommand.MANU_DCW_ARCCURRENT);

      return new DcwConfiguration
      {
        Voltage = voltage,
        HighCurrentLimit = highCurrentLimit,
        LowCurrentLimit = lowCurrentLimit,
        TestTime = testTime,
        Offset = offset,
        ArcCurrent = arcCurrent,
      };
    }

    /// <summary>
    /// Читает значение из устройства и преобразует его в double.
    /// </summary>
    /// <param name="command">Команда для запроса.</param>
    /// <returns>Преобразованное значение.</returns>
    private async Task<double> ReadDoubleAsync(ManualCommand command)
    {
      var query = $"{GetCommandSyntax(command)} ?";
      var response = await _gptModel.DeviceProtocol.QueryAsync(query, responseDelay: 100);
      return double.TryParse(response.Replace("kV", "").Replace("mA", "").Replace("S", "").Trim().Replace(".", ","), out var result) ? result : 0.0;
    }

    /// <summary>
    /// Запускает тест DCW и возвращает результат измерения тока.
    /// </summary>
    /// <returns>Измеренный ток (в мА).</returns>
    public async Task<double> MeasureCurrentAsync()
    {
      LogInformation("Запуск измерений режима ПИ DCW");
      var query = $"{FunctionCommandManager.GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} ON";
      var timeDelay = Convert.ToInt32(await GetRampTimeAsync() + await GetTestTimeAsync());

      await _gptModel.DeviceProtocol.QueryAsync(query, responseDelay: timeDelay * 1000, delayBeforeCall: delayBeforeCall);
      query = $"{FunctionCommandManager.GetCommandSyntax(FunctionCommand.MEASURE)} ?";
      var answerDevice = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 500, delayBeforeCall: delayBeforeCall);

      var result = answerDevice.Split(',');
      var measureResulte = result[3];

      LogInformation($"Результат измерения режима ПИ(DCW): {measureResulte}");

      Match match = Regex.Match(measureResulte, @"\d+(\.\d+)?");

      if (match.Success)
      {
        return double.Parse(match.Value, CultureInfo.InvariantCulture);
      }

      throw new FormatException("Число не найдено в строке.");
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
      var query = GetCommandSyntax(ManualCommand.MANU_DCW_TTIME) + "?";
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
