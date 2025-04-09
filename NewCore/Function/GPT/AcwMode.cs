using System.Globalization;
using System.Text.RegularExpressions;
using NewCore.Base.Function.Breakdown;
using NewCore.Device;
using NewCore.Function.GPT.Command;
using NewCore.Function.GPT.Data;
using static NewCore.Function.GPT.Command.FunctionCommandManager;
using static NewCore.Function.GPT.Command.ManualCommandManager;
using static Utilities.LoggerUtility;
using static AppConfiguration.Execution.ExecutionConfig;

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

    #region Mode

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetModeAsync()
    {
      LogInformation("Устанавливаем режим ACW на GPT-79904");

      if (await GetIsIdleModeEnabled())
      {
        return (true, string.Empty);
      }

      string expectedMode = "ACW";
      string command = $"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} {expectedMode}";

      // Первая попытка
      await _gptModel.DeviceProtocol.QueryAsync(command);
      var actualMode = await GetModeAsync();
      if (actualMode.Equals(expectedMode, StringComparison.OrdinalIgnoreCase))
      {
        return (true, string.Empty);
      }

      // Повторная попытка
      LogWarning("Повторная попытка установки режима ACW.");
      await _gptModel.DeviceProtocol.QueryAsync(command);
      actualMode = await GetModeAsync();
      if (actualMode.Equals(expectedMode, StringComparison.OrdinalIgnoreCase))
      {
        return (true, string.Empty);
      }

      return (false, $"Не удалось установить режим ACW. Устройство сообщает: {actualMode}");
    }

    /// <inheritdoc />
    public async Task<string> GetModeAsync()
    {
      if (await GetIsIdleModeEnabled())
        return string.Empty;

      var query = $"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} ?";
      LogInformation("Запрашиваем текущий режим...");

      var response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
      LogDebug($"Ответ на запрос режима: \"{response}\"");

      return response.Trim();
    }

    #endregion

    #region Voltage

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetVoltageAsync(double value)
    {
      LogInformation($"Устанавливаем напряжение ACW: {value:F3} В");

      if (await GetIsIdleModeEnabled())
        return (true, string.Empty);

      double kvValue = value / 1000;
      string command = $"{GetCommandSyntax(ManualCommand.MANU_ACW_VOLTAGE)} {kvValue:F3}".Replace(',', '.');

      await _gptModel.DeviceProtocol.QueryAsync(command);
      var actualKv = await GetVoltageAsync();
      if (Math.Abs(actualKv - kvValue) < 0.01)
        return (true, string.Empty);

      LogWarning("Повторная попытка установки напряжения ACW.");
      await _gptModel.DeviceProtocol.QueryAsync(command);
      actualKv = await GetVoltageAsync();
      if (Math.Abs(actualKv - kvValue) < 0.01)
        return (true, string.Empty);

      return (false, $"Не удалось установить напряжение {kvValue:F3} кВ. Устройство сообщает: {actualKv:F3} кВ.");
    }

    /// <inheritdoc />
    public async Task<double> GetVoltageAsync()
    {
      if (await GetIsIdleModeEnabled())
        return 0;

      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_VOLTAGE)} ?";
      var response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
      LogDebug($"Ответ на запрос напряжения ACW: \"{response}\"");

      if (double.TryParse(response.Replace("kV", "").Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var voltage))
      {
        return voltage * 1000; // возвращаем в вольтах
      }

      LogWarning("Не удалось разобрать напряжение. Возвращаем 0.");
      return 0;
    }

    #endregion

    #region HighCurrentLimit

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetHighCurrentLimitAsync(double value)
    {
      LogInformation($"Устанавливаем верхний предел тока ACW: {value:F3} мА");

      if (await GetIsIdleModeEnabled())
        return (true, string.Empty);

      string command = $"{GetCommandSyntax(ManualCommand.MANU_ACW_CHISET)} {value:F3}".Replace(',', '.');

      await _gptModel.DeviceProtocol.QueryAsync(command);
      var actual = await GetHighCurrentLimitAsync();
      if (Math.Abs(actual - value) < 0.1)
        return (true, string.Empty);

      LogWarning("Повторная попытка установки верхнего предела тока ACW.");
      await _gptModel.DeviceProtocol.QueryAsync(command);
      actual = await GetHighCurrentLimitAsync();
      if (Math.Abs(actual - value) < 0.1)
        return (true, string.Empty);

      return (false, $"Не удалось установить верхний предел тока {value:F3} мА. Устройство сообщает: {actual:F3} мА.");
    }

    /// <inheritdoc />
    public async Task<double> GetHighCurrentLimitAsync()
    {
      if (await GetIsIdleModeEnabled())
        return 0;

      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_CHISET)} ?";
      var response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
      LogDebug($"Ответ на запрос верхнего тока ACW: \"{response}\"");

      if (double.TryParse(response.Replace("mA", "").Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var current))
      {
        return current;
      }

      LogWarning("Не удалось разобрать верхний предел тока. Возвращаем 0.");
      return 0;
    }

    #endregion

    #region LowCurrentLimit

    /// <inheritdoc />
    /// <summary>
    /// Устанавливает нижний предел тока ACW и проверяет, что устройство приняло значение.
    /// </summary>
    /// <param name="value">Ток в миллиамперах.</param>
    /// <returns>Кортеж: bool — успех, string — сообщение об ошибке (если есть).</returns>
    public async Task<(bool Success, string Message)> SetLowCurrentLimitAsync(double value)
    {
      LogInformation($"Устанавливаем нижний предел тока ACW: {value:F3} мА");

      if (await GetIsIdleModeEnabled())
        return (true, string.Empty);

      string command = $"{GetCommandSyntax(ManualCommand.MANU_ACW_CLOSET)} {value:F3}".Replace(',', '.');

      await _gptModel.DeviceProtocol.QueryAsync(command);
      var actual = await GetLowCurrentLimitAsync();
      if (Math.Abs(actual - value) < 0.1)
        return (true, string.Empty);

      LogWarning("Повторная попытка установки нижнего предела тока ACW.");
      await _gptModel.DeviceProtocol.QueryAsync(command);
      actual = await GetLowCurrentLimitAsync();
      if (Math.Abs(actual - value) < 0.1)
        return (true, string.Empty);

      return (false, $"Не удалось установить нижний предел тока {value:F3} мА. Устройство сообщает: {actual:F3} мА.");
    }

    /// <inheritdoc />
    public async Task<double> GetLowCurrentLimitAsync()
    {
      if (await GetIsIdleModeEnabled())
        return 0;

      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_CLOSET)} ?";
      var response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
      LogDebug($"Ответ на запрос нижнего тока ACW: \"{response}\"");

      if (double.TryParse(response.Replace("mA", "").Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var current))
      {
        return current;
      }

      LogWarning("Не удалось разобрать нижний предел тока. Возвращаем 0.");
      return 0;
    }

    #endregion

    #region TestTime

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetTestTimeAsync(double value)
    {
      LogInformation($"Устанавливаем время теста ACW: {value:F1} сек");

      if (await GetIsIdleModeEnabled())
        return (true, string.Empty);

      string command = $"{GetCommandSyntax(ManualCommand.MANU_ACW_TTIME)} {value:F1}".Replace(',', '.');

      await _gptModel.DeviceProtocol.QueryAsync(command);
      var actual = await GetTestTimeAsync();
      if (Math.Abs(actual - value) < 0.1)
        return (true, string.Empty);

      LogWarning("Повторная попытка установки времени теста ACW.");
      await _gptModel.DeviceProtocol.QueryAsync(command);
      actual = await GetTestTimeAsync();
      if (Math.Abs(actual - value) < 0.1)
        return (true, string.Empty);

      return (false, $"Не удалось установить время теста {value:F1} сек. Устройство сообщает: {actual:F1} сек.");
    }

    /// <inheritdoc />
    public async Task<double> GetTestTimeAsync()
    {
      if (await GetIsIdleModeEnabled())
        return 0;

      var query = GetCommandSyntax(ManualCommand.MANU_ACW_TTIME) + "?";
      LogInformation("Запрашиваем время теста DCW...");

      string response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
      LogDebug($"Ответ на запрос DCW Test Time: \"{response}\"");

      var match = Regex.Match(response, @"\d+(\.\d+)?");
      if (match.Success && double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var testTime))
        return testTime;

      LogWarning("Не удалось разобрать значение времени теста DCW. Возвращаем 0.0.");
      return 0.0;
    }

    #endregion

    #region RampTime

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetRampTimeAsync(double value)
    {
      LogInformation($"Устанавливаем время нарастания напряжения: {value:F1} сек");

      if (await GetIsIdleModeEnabled())
        return (true, string.Empty);

      string command = $"{GetCommandSyntax(ManualCommand.MANU_RTIME)} {value:F1}".Replace(',', '.');

      await _gptModel.DeviceProtocol.QueryAsync(command);
      var actual = await GetRampTimeAsync();
      if (Math.Abs(actual - value) < 0.1)
        return (true, string.Empty);

      LogWarning("Повторная попытка установки времени нарастания напряжения.");
      await _gptModel.DeviceProtocol.QueryAsync(command);
      actual = await GetRampTimeAsync();
      if (Math.Abs(actual - value) < 0.1)
        return (true, string.Empty);

      return (false, $"Не удалось установить Ramp Time {value:F1} сек. Устройство сообщает: {actual:F1} сек.");
    }

    /// <inheritdoc />
    public async Task<double> GetRampTimeAsync()
    {
      if (await GetIsIdleModeEnabled())
        return 0;

      var query = GetCommandSyntax(ManualCommand.MANU_RTIME) + "?";
      LogInformation("Запрашиваем текущее время нарастания напряжения (Ramp Time)...");

      string response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
      LogDebug($"Ответ на запрос Ramp Time: \"{response}\"");

      var match = Regex.Match(response, @"\d+(\.\d+)?");
      if (match.Success && double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rampTime))
        return rampTime;

      LogWarning("Не удалось разобрать значение Ramp Time. Возвращаем 0.0.");
      return 0.0;
    }

    #endregion

    #region Frequency

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetFrequencyAsync(int frequency)
    {
      if (frequency != 50 && frequency != 60)
        throw new ArgumentException("Частота должна быть 50 или 60 Гц.");

      LogInformation($"Устанавливаем частоту ACW: {frequency} Гц");

      if (await GetIsIdleModeEnabled())
        return (true, string.Empty);

      string command = $"{GetCommandSyntax(ManualCommand.MANU_ACW_FREQUENCY)} {frequency}";

      await _gptModel.DeviceProtocol.QueryAsync(command);
      var actual = await GetFrequencyAsync();
      if (actual == frequency)
        return (true, string.Empty);

      LogWarning("Повторная попытка установки частоты ACW.");
      await _gptModel.DeviceProtocol.QueryAsync(command);
      actual = await GetFrequencyAsync();
      if (actual == frequency)
        return (true, string.Empty);

      return (false, $"Не удалось установить частоту {frequency} Гц. Устройство сообщает: {actual} Гц.");
    }

    /// <inheritdoc />
    /// <inheritdoc />
    public async Task<int> GetFrequencyAsync()
    {
      if (await GetIsIdleModeEnabled())
        return 0;

      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_FREQUENCY)} ?";
      var response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
      LogDebug($"Ответ на запрос частоты ACW: \"{response}\"");

      if (int.TryParse(response.Replace("Hz", "").Trim(), out var freq))
      {
        return freq;
      }

      LogWarning("Не удалось разобрать частоту. Возвращаем 0.");
      return 0;
    }

    #endregion

    #region Offset

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetOffsetAsync(double value)
    {
      LogInformation($"Устанавливаем смещение ACW: {value:F3} мА");

      if (await GetIsIdleModeEnabled())
        return (true, string.Empty);

      string command = $"{GetCommandSyntax(ManualCommand.MANU_ACW_REF)} {value:F3}".Replace(',', '.');

      await _gptModel.DeviceProtocol.QueryAsync(command);
      var actual = await GetOffsetAsync();
      if (Math.Abs(actual - value) < 0.1)
        return (true, string.Empty);

      LogWarning("Повторная попытка установки смещения ACW.");
      await _gptModel.DeviceProtocol.QueryAsync(command);
      actual = await GetOffsetAsync();
      if (Math.Abs(actual - value) < 0.1)
        return (true, string.Empty);

      return (false, $"Не удалось установить смещение {value:F3} мА. Устройство сообщает: {actual:F3} мА.");
    }

    /// <inheritdoc />
    public async Task<double> GetOffsetAsync()
    {
      if (await GetIsIdleModeEnabled())
        return 0;

      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_REF)} ?";
      var response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
      LogDebug($"Ответ на запрос смещения ACW: \"{response}\"");

      if (double.TryParse(response.Replace("mA", "").Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var offset))
      {
        return offset;
      }

      LogWarning("Не удалось разобрать смещение. Возвращаем 0.");
      return 0;
    }

    #endregion

    #region ArcCurrent

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetArcCurrentAsync(double value)
    {
      LogInformation($"Устанавливаем предельное значение дугового тока ACW: {value:F3} мА");

      if (await GetIsIdleModeEnabled())
        return (true, string.Empty);

      string command = $"{GetCommandSyntax(ManualCommand.MANU_ACW_ARCCURRENT)} {value:F3}".Replace(',', '.');

      await _gptModel.DeviceProtocol.QueryAsync(command);
      var actual = await GetArcCurrentAsync();
      if (Math.Abs(actual - value) < 0.1)
        return (true, string.Empty);

      LogWarning("Повторная попытка установки тока дугового пробоя ACW.");
      await _gptModel.DeviceProtocol.QueryAsync(command);
      actual = await GetArcCurrentAsync();
      if (Math.Abs(actual - value) < 0.1)
        return (true, string.Empty);

      return (false, $"Не удалось установить ток дуги {value:F3} мА. Устройство сообщает: {actual:F3} мА.");
    }

    /// <inheritdoc />
    public async Task<double> GetArcCurrentAsync()
    {
      if (await GetIsIdleModeEnabled())
        return 0;

      var query = $"{GetCommandSyntax(ManualCommand.MANU_ACW_ARCCURRENT)} ?";
      var response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
      LogDebug($"Ответ на запрос дугового тока ACW: \"{response}\"");

      if (double.TryParse(response.Replace("mA", "").Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var arc))
      {
        return arc;
      }

      LogWarning("Не удалось разобрать ток дуги. Возвращаем 0.");
      return 0;
    }

    #endregion

    /// <summary>
    /// Считывает текущую конфигурацию ACW.
    /// </summary>
    /// <returns>Объект <see cref="AcwConfiguration"/> с текущими параметрами.</returns>
    public async Task<AcwConfiguration> ReadConfigurationAsync()
    {
      LogInformation("Считываем конфигурацию ACW...");

      if (await GetIsIdleModeEnabled())
      {
        return new AcwConfiguration();
      }

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

    /// <inheritdoc />
    public async Task<double> MeasureCurrentAsync(double param = 0)
    {
      LogInformation("Запуск измерений режима ПИ ACW");

      if (await GetIsIdleModeEnabled())
      {
        return param;
      }

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
      if (await GetIsIdleModeEnabled())
      {
        return 0;
      }

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
      if (await GetIsIdleModeEnabled())
      {
        return 0;
      }

      var query = $"{GetCommandSyntax(command)} ?";
      var response = await _gptModel.DeviceProtocol.QueryAsync(query, 100);
      return int.Parse(response.Replace(unit, "").Trim());
    }
  }
}
