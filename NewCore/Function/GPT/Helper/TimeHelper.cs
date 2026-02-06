using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System.Globalization;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;
using static NewCore.Function.GPT.Command.FunctionCommandManager;
using static NewCore.Function.GPT.Command.ManualCommandManager;

namespace NewCore.Function.GPT.Helper
{
  static internal class TimeHelper
  {
    /// <inheritdoc />
    static public async Task<(bool Success, string Message)> SetTestTimeAsync(IBreakdownTester breakDown, BreakdownTypeMode typeCommand, double value, int delay)
    {
      LogInformation($"Начало {nameof(SetTestTimeAsync)}: value={value:F1}", isDeviceLog: true);

      try
      {
        if (ExecutionConfig.GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(SetTestTimeAsync)}: Устройство в Idle Mode. Пропускаем установку.", isDeviceLog: true);
          return (true, string.Empty);
        }

        ManualCommand manualCommand = typeCommand switch
        {
          BreakdownTypeMode.ACW => ManualCommand.MANU_ACW_TTIME,
          BreakdownTypeMode.DCW => ManualCommand.MANU_DCW_TTIME,
          BreakdownTypeMode.IR => ManualCommand.MANU_IR_TTIME,
          _ => throw new NotSupportedException($"Тип команды {typeCommand} не поддерживается в {nameof(SetTestTimeAsync)}"),
        };

        await Task.Delay(delay);
        string command = $"{GetCommandSyntax(manualCommand)} {value:F1}".Replace(',', '.');
        await breakDown.DeviceProtocol.QueryAsync(command);
        await Task.Delay(delay);

        var actual = await GetTestTimeAsync(breakDown, typeCommand, delay);
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetTestTimeAsync)}: Время теста успешно установлено.", isDeviceLog: true);
          return (true, string.Empty);
        }

        LogWarning($"{nameof(SetTestTimeAsync)}: Повторная попытка.", isDeviceLog: true);
        await breakDown.DeviceProtocol.QueryAsync(command);
        actual = await GetTestTimeAsync(breakDown, typeCommand, delay);
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetTestTimeAsync)}: Время теста установлено со второй попытки.", isDeviceLog: true);
          return (true, string.Empty);
        }

        string error = $"Не удалось установить время теста {value:F1} сек. Устройство сообщает: {actual:F1} сек.";
        LogWarning(error, isDeviceLog: true);
        return (false, error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(SetTestTimeAsync)}", ex, isDeviceLog: true);
        throw;
      }
      finally
      {
        await Task.Delay(delay);
      }
    }

    /// <inheritdoc />
    static public async Task<double> GetTestTimeAsync(IBreakdownTester breakDown, BreakdownTypeMode typeCommand, int delay)
    {
      LogInformation($"Начало {nameof(GetTestTimeAsync)}", isDeviceLog: true);

      try
      {
        if (ExecutionConfig.GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(GetTestTimeAsync)}: Устройство в Idle Mode. Возвращаем 0.", isDeviceLog: true);
          return 0;
        }

        ManualCommand manualCommand = typeCommand switch
        {
          BreakdownTypeMode.ACW => ManualCommand.MANU_ACW_TTIME,
          BreakdownTypeMode.DCW => ManualCommand.MANU_DCW_TTIME,
          BreakdownTypeMode.IR => ManualCommand.MANU_IR_TTIME,
          _ => throw new NotSupportedException($"Тип команды {typeCommand} не поддерживается в {nameof(SetTestTimeAsync)}"),
        };

        var query = GetCommandSyntax(manualCommand) + "?";
        var response = await breakDown.DeviceProtocol.QueryAsync(query, timeout: 1000);
        LogDebug($"Ответ на {nameof(GetTestTimeAsync)}: \"{response}\"", isDeviceLog: true);

        var match = Regex.Match(response, @"\d+(\.\d+)?");
        if (match.Success && double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var testTime))
        {
          LogInformation($"{nameof(GetTestTimeAsync)}: Результат = {testTime}", isDeviceLog: true);
          return testTime;
        }

        LogWarning($"{nameof(GetTestTimeAsync)}: Не удалось разобрать время. Возвращаем -1.", isDeviceLog: true);
        return -1;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(GetTestTimeAsync)}", ex, isDeviceLog: true);
        throw;
      }
    }

    /// <inheritdoc />
    static public async Task<(bool Success, string Message)> SetRampTimeAsync(IBreakdownTester breakDown, double value, int delay)
    {
      LogInformation($"Начало {nameof(SetRampTimeAsync)}: value={value:F1}", isDeviceLog: true);

      try
      {
        if (ExecutionConfig.GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(SetRampTimeAsync)}: Устройство в Idle Mode. Пропускаем установку.", isDeviceLog: true);
          return (true, string.Empty);
        }

        await Task.Delay(delay);
        string command = $"{GetCommandSyntax(ManualCommand.MANU_RTIME)} {value:F1}".Replace(',', '.');
        await breakDown.DeviceProtocol.QueryAsync(command);
        await Task.Delay(delay);

        var actual = await GetRampTimeAsync(breakDown, delay);
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetRampTimeAsync)}: Ramp Time успешно установлен.", isDeviceLog: true);
          return (true, string.Empty);
        }

        LogWarning($"{nameof(SetRampTimeAsync)}: Повторная попытка.", isDeviceLog: true);
        await breakDown.DeviceProtocol.QueryAsync(command);
        actual = await GetRampTimeAsync(breakDown, delay);
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetRampTimeAsync)}: Ramp Time установлен со второй попытки.", isDeviceLog: true);
          return (true, string.Empty);
        }

        string error = $"Не удалось установить Ramp Time {value:F1} сек. Устройство сообщает: {actual:F1} сек.";
        LogWarning(error, isDeviceLog: true);
        return (false, error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(SetRampTimeAsync)}", ex, isDeviceLog: true);
        throw;
      }
      finally
      {
        await Task.Delay(delay);
      }
    }

    /// <inheritdoc />
    static public async Task<double> GetRampTimeAsync(IBreakdownTester breakDown, int delay)
    {
      LogInformation($"Начало {nameof(GetRampTimeAsync)}", isDeviceLog: true);

      try
      {
        if (ExecutionConfig.GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(GetRampTimeAsync)}: Устройство в Idle Mode. Возвращаем 0.", isDeviceLog: true);
          return 0;
        }

        var query = GetCommandSyntax(ManualCommand.MANU_RTIME) + "?";
        var response = await breakDown.DeviceProtocol.QueryAsync(query, timeout: 1000);
        LogDebug($"Ответ на {nameof(GetRampTimeAsync)}: \"{response}\"", isDeviceLog: true);

        var match = Regex.Match(response, @"\d+(\.\d+)?");
        if (match.Success && double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rampTime))
        {
          LogInformation($"{nameof(GetRampTimeAsync)}: Результат = {rampTime}", isDeviceLog: true);
          return rampTime;
        }

        LogWarning($"{nameof(GetRampTimeAsync)}: Не удалось разобрать Ramp Time. Возвращаем 0.", isDeviceLog: true);
        return 0.0;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(GetRampTimeAsync)}", ex, isDeviceLog: true);
        throw;
      }
    }
  }
}
