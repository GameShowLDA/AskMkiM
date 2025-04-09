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
  /// Класс для управления режимом IR (Insulation Resistance).
  /// </summary>
  public class IrMode : IIrModeBreakdown
  {
    private GPT79904 _gptModel { get; set; }
    private static double timeDelay = 2;
    private static int delayBeforeCall = 100;

    public IrMode(GPT79904 gpt79904) => _gptModel = gpt79904;

    #region Mode

    public async Task<(bool Success, string Message)> SetModeAsync()
    {
      LogInformation("Устанавливаем режим IR на GPT-79904");

      if (await GetIsIdleModeEnabled()) return (true, string.Empty);

      string command = $"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} IR";

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        await _gptModel.DeviceProtocol.QueryAsync(command);
        string check = await _gptModel.DeviceProtocol.QueryAsync($"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} ?", timeout: 1000);
        if (check.Trim().Equals("IR", StringComparison.OrdinalIgnoreCase))
          return (true, string.Empty);

        LogWarning($"Попытка {attempt} неудачна. Ответ: {check}");
      }

      return (false, "Не удалось установить режим IR после 2 попыток.");
    }

    public async Task<string> GetModeAsync()
    {
      var response = await _gptModel.DeviceProtocol.QueryAsync($"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} ?", timeout: 1000);
      return response.Trim();
    }

    #endregion

    #region Voltage

    public async Task<(bool Success, string Message)> SetVoltageAsync(double value)
    {
      LogInformation($"Устанавливаем напряжение IR: {value} В");

      if (await GetIsIdleModeEnabled()) return (true, string.Empty);

      string kv = (value / 1000).ToString("F3", CultureInfo.InvariantCulture);
      string command = $"{GetCommandSyntax(ManualCommand.MANU_IR_VOLTAGE)} {kv}";

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        await _gptModel.DeviceProtocol.QueryAsync(command);
        double actual = await GetVoltageAsync();
        if (Math.Abs(actual - value) < 1.0)
          return (true, string.Empty);

        LogWarning($"Попытка {attempt} установки напряжения IR неудачна. Ответ: {actual} В");
      }

      return (false, $"Не удалось установить напряжение IR: {value} В.");
    }

    public async Task<double> GetVoltageAsync()
    {
      var response = await _gptModel.DeviceProtocol.QueryAsync($"{GetCommandSyntax(ManualCommand.MANU_IR_VOLTAGE)} ?", timeout: 1000);
      if (double.TryParse(response.Replace("kV", "").Trim().Replace(".", ","), out var kv))
        return kv * 1000;

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

        LogWarning($"Попытка {attempt} установки высокого предела сопротивления неудачна. Ответ: {actual} ГОм");
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

        LogWarning($"Попытка {attempt} установки нижнего предела сопротивления неудачна. Ответ: {actual} МОм");
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
      LogInformation($"Устанавливаем время теста IR: {value} сек");

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

        LogWarning($"Попытка {attempt} установки времени теста IR неудачна. Ответ: {actual} сек");
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
      LogInformation($"Устанавливаем смещение IR: {value} ГОм");

      if (await GetIsIdleModeEnabled()) return (true, string.Empty);

      string command = $"{GetCommandSyntax(ManualCommand.MANU_IR_REF)} {value}M";

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        await _gptModel.DeviceProtocol.QueryAsync(command);
        double actual = await GetOffsetAsync();
        if (Math.Abs(actual - value) < 0.1)
          return (true, string.Empty);

        LogWarning($"Попытка {attempt} установки смещения неудачна. Ответ: {actual} ГОм");
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

    public async Task<double> MeasureResistanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1)
    {
      if (await GetIsIdleModeEnabled()) return param;

      var testCommand = $"{GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} ON";
      await _gptModel.DeviceProtocol.QueryAsync(testCommand, responseDelay: timeDelay * 1000, delayBeforeCall: delayBeforeCall);

      string response = await _gptModel.DeviceProtocol.QueryAsync($"{GetCommandSyntax(FunctionCommand.MEASURE)} ?", timeout: 500, delayBeforeCall: delayBeforeCall);
      var parts = response.Split(',');

      string raw = parts.ElementAtOrDefault(3)?.ToLower() ?? throw new FormatException("Нет результата измерения.");
      double multiplier = raw.EndsWith("gohm") ? 1000 : raw.EndsWith("mohm") ? 1 : raw.EndsWith("kohm") ? 0.001 : throw new FormatException("Неизвестный формат.");
      raw = Regex.Replace(raw, @"[^0-9.,]", "").Replace('.', ',');

      return double.TryParse(raw, out var value) ? value * multiplier : throw new FormatException("Ошибка преобразования значения сопротивления.");
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

    #endregion
  }
}
