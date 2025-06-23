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
  /// Класс для управления режимом DCW.
  /// </summary>
  public class DcwMode : IDcwModeBreakdown
  {
    private GPT79904 _gptModel { get; set; }
    static private int delayBeforeCall = 100;

    int delay = 50;

    public DcwMode(GPT79904 gpt79904) => _gptModel = gpt79904;

    #region Mode

    public async Task<(bool Success, string Message)> SetModeAsync()
    {
      LogInformation("Устанавливаем режим DCW на GPT-79904", isDeviceLog: true);

      if (await GetIsIdleModeEnabled())
      {
        return (true, string.Empty);
      }
      var command = $"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} DCW";

      await _gptModel.DeviceProtocol.QueryAsync(command);
      await Task.Delay(delay);

      string response = await GetModeAsync();
      if (response.Trim().Equals("DCW", StringComparison.OrdinalIgnoreCase))
      {
        return (true, string.Empty);
      }

      LogWarning("Повторная попытка установки режима DCW.", isDeviceLog: true);
      await _gptModel.DeviceProtocol.QueryAsync(command);
      response = await GetModeAsync();
      if (response.Trim().Equals("DCW", StringComparison.OrdinalIgnoreCase))
      {
        return (true, string.Empty);
      }

      return (false, $"Устройство не приняло режим DCW, ответ: {response}");
    }

    public async Task<string> GetModeAsync()
    {
      await Task.Delay(50);
      var query = $"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} ?";
      var response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);
      return response.Trim();
    }

    #endregion

    #region Voltage

    public async Task<(bool Success, string Message)> SetVoltageAsync(double value)
    {
      LogInformation($"Устанавливаем напряжение DCW: {value:F3} В", isDeviceLog: true);

      if (await GetIsIdleModeEnabled())
      {
        return (true, string.Empty);
      }

      double kvValue = value / 1000;
      string command = $"{GetCommandSyntax(ManualCommand.MANU_DCW_VOLTAGE)} {kvValue:F3}".Replace(',', '.');

      await _gptModel.DeviceProtocol.QueryAsync(command);
      await Task.Delay(delay);

      var actualKv = await GetVoltageAsync();
      if (actualKv.HasValue && Math.Abs(actualKv.Value - kvValue) < 0.01)
      {
        return (true, string.Empty);
      }

      LogWarning("Повторная попытка установки напряжения DCW.", isDeviceLog: true);
      await _gptModel.DeviceProtocol.QueryAsync(command);
      actualKv = await GetVoltageAsync();
      if (actualKv.HasValue && Math.Abs(actualKv.Value - kvValue) < 0.01)
      {
        return (true, string.Empty);
      }

      return (false, $"Не удалось установить напряжение {kvValue:F3} кВ. Устройство сообщает: {actualKv?.ToString("F3") ?? "недоступно"} кВ.");
    }

    public async Task<double?> GetVoltageAsync()
    {
      string query = GetCommandSyntax(ManualCommand.MANU_DCW_VOLTAGE) + "?";
      string response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 500);

      response = response.Trim().Replace("kV", "", StringComparison.OrdinalIgnoreCase).Trim();
      if (double.TryParse(response.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
        return result;

      LogError($"Ошибка при чтении напряжения DCW: '{response}' не удалось распарсить.", isDeviceLog: true);
      return null;
    }

    #endregion

    #region HighCurrentLimit

    public async Task<(bool Success, string Message)> SetHighCurrentLimitAsync(double value)
    {
      LogInformation($"Устанавливаем верхний предел тока DCW: {value:F3} мА", isDeviceLog: true);

      if (await GetIsIdleModeEnabled())
      {
        return (true, string.Empty);
      }

      string command = $"{GetCommandSyntax(ManualCommand.MANU_DCW_CHISET)} {value:F3}".Replace(',', '.');

      await _gptModel.DeviceProtocol.QueryAsync(command);
      await Task.Delay(delay);

      double actual = await GetHighCurrentLimitAsync();
      if (Math.Abs(actual - value) < 0.1)
      {
        return (true, string.Empty);
      }

      LogWarning("Повторная попытка установки верхнего предела тока DCW.", isDeviceLog: true);
      await _gptModel.DeviceProtocol.QueryAsync(command);
      actual = await GetHighCurrentLimitAsync();
      if (Math.Abs(actual - value) < 0.1)
      {
        return (true, string.Empty);
      }

      return (false, $"Устройство сообщает: {actual:F3} мА. Ожидалось: {value:F3} мА.");
    }

    public async Task<double> GetHighCurrentLimitAsync() => await ReadDoubleAsync(ManualCommand.MANU_DCW_CHISET);

    #endregion

    #region LowCurrentLimit

    public async Task<(bool Success, string Message)> SetLowCurrentLimitAsync(double value)
    {
      LogInformation($"Устанавливаем нижний предел тока DCW: {value:F3} мА", isDeviceLog: true);

      if (await GetIsIdleModeEnabled())
      {
        return (true, string.Empty);
      }

      string command = $"{GetCommandSyntax(ManualCommand.MANU_DCW_CLOSET)} {value:F3}".Replace(',', '.');

      await _gptModel.DeviceProtocol.QueryAsync(command);
      await Task.Delay(delay);

      double actual = await GetLowCurrentLimitAsync();
      if (Math.Abs(actual - value) < 0.1)
      {
        return (true, string.Empty);
      }

      LogWarning("Повторная попытка установки нижнего предела тока DCW.", isDeviceLog: true);
      await _gptModel.DeviceProtocol.QueryAsync(command);
      actual = await GetLowCurrentLimitAsync();
      if (Math.Abs(actual - value) < 0.1)
      {
        return (true, string.Empty);
      }

      return (false, $"Устройство сообщает: {actual:F3} мА. Ожидалось: {value:F3} мА.");
    }

    public async Task<double> GetLowCurrentLimitAsync() => await ReadDoubleAsync(ManualCommand.MANU_DCW_CLOSET);

    #endregion

    #region TestTime

    public async Task<(bool Success, string Message)> SetTestTimeAsync(double value)
    {
      LogInformation($"Устанавливаем время теста DCW: {value:F1} сек", isDeviceLog: true);

      if (await GetIsIdleModeEnabled())
      {
        return (true, string.Empty);
      }

      string command = $"{GetCommandSyntax(ManualCommand.MANU_DCW_TTIME)} {value:F1}".Replace(',', '.');
      await _gptModel.DeviceProtocol.QueryAsync(command);
      await Task.Delay(delay);

      double actual = await GetTestTimeAsync();
      if (Math.Abs(actual - value) < 0.1)
      {
        return (true, string.Empty);
      }

      LogWarning("Повторная попытка установки времени теста DCW.", isDeviceLog: true);
      await _gptModel.DeviceProtocol.QueryAsync(command);
      actual = await GetTestTimeAsync();
      if (Math.Abs(actual - value) < 0.1)
      {
        return (true, string.Empty);
      }

      return (false, $"Устройство сообщает: {actual:F1} сек. Ожидалось: {value:F1} сек.");
    }

    public async Task<double> GetTestTimeAsync()
    {
      string query = GetCommandSyntax(ManualCommand.MANU_DCW_TTIME) + "?";
      string response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);

      var match = Regex.Match(response, @"\d+(\.\d+)?");
      return match.Success && double.TryParse(match.Value, CultureInfo.InvariantCulture, out var result) ? result : 0.0;
    }

    #endregion

    #region RampTime

    public async Task<(bool Success, string Message)> SetRampTimeAsync(double value)
    {
      LogInformation($"Устанавливаем Ramp Time: {value:F1} сек", isDeviceLog: true);

      if (await GetIsIdleModeEnabled())
        return (true, string.Empty);

      string command = $"{GetCommandSyntax(ManualCommand.MANU_RTIME)} {value:F1}".Replace(',', '.');
      await _gptModel.DeviceProtocol.QueryAsync(command);
      await Task.Delay(delay);

      double actual = await GetRampTimeAsync();
      if (Math.Abs(actual - value) < 0.1)
        return (true, string.Empty);

      LogWarning("Повторная попытка установки Ramp Time DCW.", isDeviceLog: true);
      await _gptModel.DeviceProtocol.QueryAsync(command);
      actual = await GetRampTimeAsync();
      if (Math.Abs(actual - value) < 0.1)
        return (true, string.Empty);

      return (false, $"Устройство сообщает: {actual:F1} сек. Ожидалось: {value:F1} сек.");
    }

    public async Task<double> GetRampTimeAsync()
    {
      var query = GetCommandSyntax(ManualCommand.MANU_RTIME) + "?";
      string response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 1000);

      var match = Regex.Match(response, @"\d+(\.\d+)?");
      return match.Success && double.TryParse(match.Value, CultureInfo.InvariantCulture, out var result) ? result : 0.0;
    }

    #endregion

    #region Offset

    public async Task<(bool Success, string Message)> SetOffsetAsync(double value)
    {
      LogInformation($"Устанавливаем смещение DCW: {value:F3} мА", isDeviceLog: true);

      if (await GetIsIdleModeEnabled())
        return (true, string.Empty);

      string command = $"{GetCommandSyntax(ManualCommand.MANU_DCW_REF)} {value:F3}".Replace(',', '.');
      await _gptModel.DeviceProtocol.QueryAsync(command);
      await Task.Delay(delay);

      double actual = await GetOffsetAsync();
      if (Math.Abs(actual - value) < 0.1)
        return (true, string.Empty);

      LogWarning("Повторная попытка установки смещения DCW.", isDeviceLog: true);
      await _gptModel.DeviceProtocol.QueryAsync(command);
      actual = await GetOffsetAsync();
      if (Math.Abs(actual - value) < 0.1)
        return (true, string.Empty);

      return (false, $"Устройство сообщает: {actual:F3} мА. Ожидалось: {value:F3} мА.");
    }

    public async Task<double> GetOffsetAsync() => await ReadDoubleAsync(ManualCommand.MANU_DCW_REF);

    #endregion

    #region ArcCurrent

    public async Task<(bool Success, string Message)> SetArcCurrentAsync(double value)
    {
      LogInformation($"Устанавливаем ток дуги DCW: {value:F3} мА", isDeviceLog: true);

      if (await GetIsIdleModeEnabled())
        return (true, string.Empty);

      string command = $"{GetCommandSyntax(ManualCommand.MANU_DCW_ARCCURRENT)} {value:F3}".Replace(',', '.');
      await _gptModel.DeviceProtocol.QueryAsync(command);
      await Task.Delay(delay);

      double actual = await GetArcCurrentAsync();
      if (Math.Abs(actual - value) < 0.1)
        return (true, string.Empty);

      LogWarning("Повторная попытка установки тока дуги DCW.", isDeviceLog: true);
      await _gptModel.DeviceProtocol.QueryAsync(command);
      actual = await GetArcCurrentAsync();
      if (Math.Abs(actual - value) < 0.1)
        return (true, string.Empty);

      return (false, $"Устройство сообщает: {actual:F3} мА. Ожидалось: {value:F3} мА.");
    }

    public async Task<double> GetArcCurrentAsync() => await ReadDoubleAsync(ManualCommand.MANU_DCW_ARCCURRENT);

    #endregion

    #region Configuration & Measurement

    public async Task<DcwConfiguration> ReadConfigurationAsync()
    {
      LogInformation("Считываем конфигурацию DCW...", isDeviceLog: true);

      return new DcwConfiguration
      {
        Voltage = (await GetVoltageAsync()) ?? 0,
        HighCurrentLimit = await GetHighCurrentLimitAsync(),
        LowCurrentLimit = await GetLowCurrentLimitAsync(),
        TestTime = await GetTestTimeAsync(),
        Offset = await GetOffsetAsync(),
        ArcCurrent = await GetArcCurrentAsync(),
      };
    }

    public async Task<double> MeasureCurrentAsync()
    {
      LogInformation("Запуск измерений режима DCW", isDeviceLog: true);

      if (await GetIsIdleModeEnabled())
        return 0;

      var query = $"{GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} ON";
      int delay = (int)(await GetRampTimeAsync() + await GetTestTimeAsync()) * 1000;

      await _gptModel.DeviceProtocol.QueryAsync(query, responseDelay: delay, delayBeforeCall: delayBeforeCall);
      await Task.Delay(delay);

      query = $"{GetCommandSyntax(FunctionCommand.MEASURE)} ?";

      string[] result;
      do
      {
        var response = await _gptModel.DeviceProtocol.QueryAsync(query, timeout: 500, delayBeforeCall: delayBeforeCall);
        result = response.Split(',');
      } while (result.Length <= 1);

      var measure = result[3];
      Match match = Regex.Match(measure, @"\d+(\.\d+)?");
      return match.Success ? double.Parse(match.Value, CultureInfo.InvariantCulture) : throw new FormatException("Число не найдено.");
    }

    #endregion

    private async Task<double> ReadDoubleAsync(ManualCommand command)
    {
      if (await GetIsIdleModeEnabled())
        return 0;

      var query = $"{GetCommandSyntax(command)} ?";
      var response = await _gptModel.DeviceProtocol.QueryAsync(query, responseDelay: 100, timeout: 1000);
      return double.TryParse(response.Replace("kV", "").Replace("mA", "").Replace("S", "").Trim().Replace(".", ","), out var result)
        ? result
        : 0.0;
    }
  }
}
