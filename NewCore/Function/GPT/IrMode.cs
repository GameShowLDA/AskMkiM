using System.Globalization;
using System.Text.RegularExpressions;
using NewCore.Base.Function.Breakdown;
using NewCore.Device;
using NewCore.Function.GPT.Data;
using static AppConfiguration.Execution.ExecutionConfig;
using static NewCore.Function.GPT.Command.FunctionCommandManager;
using static NewCore.Function.GPT.Command.ManualCommandManager;
using static Utilities.LoggerUtility;

namespace NewCore.Function.GPT
{
  /// <summary>
  /// Класс для управления режимом IR (Insulation Resistance).
  /// </summary>
  public class IrMode : IIrModeBreakdown
  {
    private GPT79904 _gptModel { get; set; }
    private static double timeDelay = 2;
    private static int delayBeforeCall = 100;

    public IrMode(GPT79904 gpt79904) => _gptModel = gpt79904;

    #region Mode

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetModeAsync()
    {
      LogInformation("Устанавливаем режим IR на GPT-79904", isDeviceLog: true);

      if (await GetIsIdleModeEnabled()) return (true, string.Empty);

      string command = $"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} IR";
      if ((await GetModeAsync()).Success)
      {
        return (true, string.Empty);
      }

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        await _gptModel.DeviceProtocol.QueryAsync(command);
        var check = await GetModeAsync();

        if (check.Success)
        {
          return (true, string.Empty);
        }

        LogWarning($"Попытка {attempt} неудачна. Ответ: {check.Message}", isDeviceLog: true);
      }

      return (false, "Не удалось установить режим IR после 2 попыток.");
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> GetModeAsync()
    {
      var response = await _gptModel.DeviceProtocol.QueryAsync($"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} ?", timeout: 1000);
      var actual = response.Trim();
      return actual.Equals("IR", StringComparison.OrdinalIgnoreCase)
        ? (true, actual)
        : (false, actual);
    }

    #endregion

    #region Voltage

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetVoltageAsync(double value)
    {
      LogInformation($"Устанавливаем напряжение IR: {value} В", isDeviceLog: true);

      if (await GetIsIdleModeEnabled())
        return (true, string.Empty);

      double actual = await GetVoltageAsync();
      if (Math.Abs(actual - value) < 1.0)
        return (true, string.Empty);


      string kvFormatted = (value / 1000).ToString("F3", CultureInfo.InvariantCulture);
      string command = $"{GetCommandSyntax(ManualCommand.MANU_IR_VOLTAGE)} {kvFormatted}";

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        await _gptModel.DeviceProtocol.QueryAsync(command);
        actual = await GetVoltageAsync();

        if (Math.Abs(actual - value) < 1.0)
          return (true, string.Empty);

        LogWarning($"Попытка {attempt} установки напряжения не удалась. Получено: {actual:F1} В, ожидалось: {value:F1} В.", isDeviceLog: true);
      }

      return (false, $"Не удалось установить напряжение IR. Устройство не приняло значение {value} В.");
    }

    /// <inheritdoc />
    public async Task<double> GetVoltageAsync()
    {
      string query = $"{GetCommandSyntax(ManualCommand.MANU_IR_VOLTAGE)} ?";
      string response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);

      string cleaned = response.Replace("kV", "").Trim().Replace(".", ",");

      if (double.TryParse(cleaned, out var kv))
        return kv * 1000;

      LogError($"Ошибка чтения напряжения IR. Ответ: '{response}'", isDeviceLog: true);
      return -1;
    }

    #endregion

    #region HighResistanceLimit

    public async Task<(bool Success, string Message)> SetHighResistanceLimitAsync(double value)
    {
      if (await GetIsIdleModeEnabled()) return (true, string.Empty);

      string command = $"{GetCommandSyntax(ManualCommand.MANU_IR_RHISET)} {value:F3}".Replace(',', '.');

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        await _gptModel.DeviceProtocol.QueryAsync(command);
        double actual = await GetHighResistanceLimitAsync();
        if (Math.Abs(actual - value) < 0.1)
          return (true, string.Empty);

        LogWarning($"Попытка {attempt} установки высокого предела сопротивления неудачна. Ответ: {actual} ГОм", isDeviceLog: true);
      }

      return (false, $"Не удалось установить высокий предел IR: {value} ГОм.");
    }

    public async Task<double> GetHighResistanceLimitAsync()
    {
      var response = await _gptModel.DeviceProtocol.QueryAsync($"{GetCommandSyntax(ManualCommand.MANU_IR_RHISET)} ?", timeout: 1000);
      return ParseDouble(response, "G");
    }

    #endregion

    #region LowResistanceLimit

    public async Task<(bool Success, string Message)> SetLowResistanceLimitAsync(double value)
    {
      if (await GetIsIdleModeEnabled()) return (true, string.Empty);
      if (value == 1000) value = 999;

      string command = $"{GetCommandSyntax(ManualCommand.MANU_IR_RLOSET)} {value:F0}M";

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        await _gptModel.DeviceProtocol.QueryAsync(command);
        double actual = await GetLowResistanceLimitAsync();
        if (Math.Abs(actual - value) < 0.5)
          return (true, string.Empty);

        LogWarning($"Попытка {attempt} установки нижнего предела сопротивления неудачна. Ответ: {actual} МОм", isDeviceLog: true);
      }

      return (false, $"Не удалось установить нижний предел IR: {value} МОм.");
    }

    public async Task<double> GetLowResistanceLimitAsync()
    {
      var response = await _gptModel.DeviceProtocol.QueryAsync($"{GetCommandSyntax(ManualCommand.MANU_IR_RLOSET)} ?", timeout: 1000);
      return ParseDouble(response, "M");
    }

    #endregion

    #region TestTime

    public async Task<(bool Success, string Message)> SetTestTimeAsync(double value)
    {
      LogInformation($"Устанавливаем время теста IR: {value} сек", isDeviceLog: true);

      if (await GetIsIdleModeEnabled()) return (true, string.Empty);

      string command = $"{GetCommandSyntax(ManualCommand.MANU_IR_TTIME)} {value:F1}".Replace(',', '.');

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        await _gptModel.DeviceProtocol.QueryAsync(command);
        double actual = await GetTestTimeAsync();
        if (Math.Abs(actual - value) < 0.5)
        {
          timeDelay = value;
          return (true, string.Empty);
        }

        LogWarning($"Попытка {attempt} установки времени теста IR неудачна. Ответ: {actual} сек", isDeviceLog: true);
      }

      return (false, $"Не удалось установить время теста IR: {value} сек.");
    }

    public async Task<double> GetTestTimeAsync()
    {
      var response = await _gptModel.DeviceProtocol.QueryAsync($"{GetCommandSyntax(ManualCommand.MANU_IR_TTIME)} ?", timeout: 1000);
      return ParseDouble(response, "S");
    }

    #endregion

    #region Offset

    public async Task<(bool Success, string Message)> SetOffsetAsync(double value)
    {
      LogInformation($"Устанавливаем смещение IR: {value} ГОм", isDeviceLog: true);

      if (await GetIsIdleModeEnabled()) return (true, string.Empty);

      string command = $"{GetCommandSyntax(ManualCommand.MANU_IR_REF)} {value}M";

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        await _gptModel.DeviceProtocol.QueryAsync(command);
        double actual = await GetOffsetAsync();
        if (Math.Abs(actual - value) < 0.1)
          return (true, string.Empty);

        LogWarning($"Попытка {attempt} установки смещения неудачна. Ответ: {actual} ГОм", isDeviceLog: true);
      }

      return (false, $"Не удалось установить смещение IR: {value} ГОм.");
    }

    public async Task<double> GetOffsetAsync()
    {
      var response = await _gptModel.DeviceProtocol.QueryAsync($"{GetCommandSyntax(ManualCommand.MANU_IR_REF)} ?", timeout: 1000);
      return ParseDouble(response, "M");
    }

    #endregion

    #region Measure & Config


    public async Task StopMeasure()
    {
      string response = await _gptModel.DeviceProtocol.QueryAsync($"{GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} OFF");
      await _gptModel.DeviceProtocol.QueryAsync(response);
    }


    public async Task<double> MeasureResistanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1)
    {
      if (await GetIsIdleModeEnabled()) return param;

      await StopMeasure();
      await Task.Delay(delayBeforeCall);

      int totalTicks = (int)((timeDelay * 1000) / 200) - 1;
      var timer = new System.Timers.Timer();
      timer.Interval = 200;
      timer.AutoReset = true;
      int tickCount = 0;
      string response = string.Empty;
      var testCommand = $"{GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} ON";

      var model = new MeasurementData();

      timer.Elapsed += async (s, a) =>
       {
         tickCount++;

         await Task.Delay(100);
         response = await _gptModel.DeviceProtocol.QueryAsync($"{GetCommandSyntax(FunctionCommand.MEASURE)} ?", timeout: 500, delayBeforeCall: delayBeforeCall);
         try
         {

           model = ParseMeasurement(response);
           if (model.Status.ToLower().Contains("fail"))
           {
             await _gptModel.DeviceProtocol.QueryAsync(testCommand);
           }
           else if (model.Status.ToLower().Contains("test") && model.Resistance > 0 && model.Resistance > param)
           {
             await _gptModel.IrManger.StopMeasure();
             tickCount = totalTicks + 1;
             timer.Stop();
             return;
           }
         }
         catch
         {

         }

         model = null;
       };

      await _gptModel.DeviceProtocol.QueryAsync(testCommand);
      timer.Start();

      var task = Task.Run(async () =>
      {
        while (tickCount <= totalTicks)
        {
          await Task.Delay(1);
        }
      });

      Task.WaitAny(task);

      timer.Stop();
      timer.Dispose();

      while (true)
      {
        response = await _gptModel.DeviceProtocol.QueryAsync($"{GetCommandSyntax(FunctionCommand.MEASURE)} ?", timeout: 500, delayBeforeCall: delayBeforeCall);
        if (!response.ToLower().Contains("test"))
        {
          break;
        }

        await Task.Delay(50);
      }

      response = await _gptModel.DeviceProtocol.QueryAsync($"{GetCommandSyntax(FunctionCommand.MEASURE)} ?", timeout: 500, delayBeforeCall: delayBeforeCall);
      var parts = response.Split(',');

      string raw = parts.ElementAtOrDefault(3)?.ToLower() ?? throw new FormatException("Нет результата измерения.");
      double multiplier = raw.EndsWith("gohm") ? 1000 : raw.EndsWith("mohm") ? 1 : raw.EndsWith("kohm") ? 0.001 : throw new FormatException("Неизвестный формат.");
      raw = Regex.Replace(raw, @"[^0-9.,]", "").Replace('.', ',');
      double value = -1;

      while (!double.TryParse(raw, out value))
      {
        response = await _gptModel.DeviceProtocol.QueryAsync($"{GetCommandSyntax(FunctionCommand.MEASURE)} ?", timeout: 500, delayBeforeCall: delayBeforeCall);
        parts = response.Split(',');
        raw = parts.ElementAtOrDefault(3)?.ToLower() ?? throw new FormatException("Нет результата измерения.");
        multiplier = raw.EndsWith("gohm") ? 1000 : raw.EndsWith("mohm") ? 1 : raw.EndsWith("kohm") ? 0.001 : throw new FormatException("Неизвестный формат.");
        raw = Regex.Replace(raw, @"[^0-9.,]", "").Replace('.', ',');
      }

      return double.TryParse(raw, out value) ? value * multiplier : throw new FormatException("Ошибка преобразования значения сопротивления.");
    }

    public async Task<IrConfiguration> ReadConfigurationAsync()
    {
      return new IrConfiguration
      {
        Voltage = await GetVoltageAsync(),
        HighResistanceLimit = await GetHighResistanceLimitAsync(),
        LowResistanceLimit = await GetLowResistanceLimitAsync(),
        TestTime = await GetTestTimeAsync(),
        Offset = await GetOffsetAsync()
      };
    }

    public List<int> GetVoltagesForResistance(double resistance)
    {
      return resistance switch
      {
        <= 0.3 => new() { 50, 100 },
        <= 1.0 => new() { 100, 200 },
        <= 3.0 => new() { 200, 500 },
        <= 1000.0 => new() { 200, 500 },
        _ => throw new ArgumentOutOfRangeException(nameof(resistance), "Сопротивление вне диапазона.")
      };
    }

    #endregion

    #region Helpers

    private double ParseDouble(string input, string suffix)
    {
      string cleaned = input.Replace(suffix, "").Trim().Replace(".", ",");
      return double.TryParse(cleaned, out var result) ? result : 0.0;
    }

    private static MeasurementData ParseMeasurement(string input)
    {
      // Разделяем строку по запятым
      var parts = input.Split(',');

      if (parts.Length != 5)
      {
        throw new ArgumentException("Неверный формат входящей строки");
      }

      try
      {
        string raw = parts.ElementAtOrDefault(3)?.ToLower() ?? throw new FormatException("Нет результата измерения.");
        double multiplier = raw.EndsWith("gohm") ? 1000 : raw.EndsWith("mohm") ? 1 : raw.EndsWith("kohm") ? 0.001 : throw new FormatException("Неизвестный формат.");
        raw = Regex.Replace(raw, @"[^0-9.,]", "").Replace('.', ',');
        double value = -1;

        return new MeasurementData()
        {
          Mode = parts[0].Trim(),
          Status = parts[1].Trim(),
          Resistance = double.TryParse(raw, out value) ? value * multiplier : -1
        };
      }
      catch
      {
        throw new FormatException("Ошибка преобразования значения сопротивления.");
      }
    }


    private class MeasurementData
    {
      /// <summary>
      /// Режим измерения.
      /// </summary>
      public string Mode { get; set; }

      /// <summary>
      /// Статус теста.
      /// </summary>
      public string Status { get; set; }

      /// <summary>
      ///  Напряжение (переводится из строкового формата).
      /// </summary>
      public double Voltage { get; set; }

      /// <summary>
      /// Сопротивление (также преобразуется из строки).
      /// </summary>
      public double Resistance { get; set; }

      /// <summary>
      ///  Время, прошедшее с начала теста (преобразуемое значение времени).
      /// </summary>
      public TimeSpan ElapsedTime { get; set; }
    }

    #endregion
  }
}
