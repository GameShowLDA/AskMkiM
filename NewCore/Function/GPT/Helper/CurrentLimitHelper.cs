using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System.Globalization;
using static Ask.LogLib.LoggerUtility;
using static NewCore.Function.GPT.Command.FunctionCommandManager;
using static NewCore.Function.GPT.Command.ManualCommandManager;

namespace NewCore.Function.GPT.Helper
{
  static internal class CurrentLimitHelper
  {
    /// <inheritdoc />
    static public async Task<(bool Success, string Message)> SetHighCurrentLimitAsync(IBreakdownTester breakDown, BreakdownTypeMode typeCommand, double value, int delay)
    {
      LogInformation($"Начало {nameof(SetHighCurrentLimitAsync)}: value={value:F3}", isDeviceLog: true);

      try
      {
        if (await ExecutionConfig.GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(SetHighCurrentLimitAsync)}: Устройство в Idle Mode. Пропускаем установку.", isDeviceLog: true);
          return (true, string.Empty);
        }

        ManualCommand manualCommand = typeCommand switch
        {
          BreakdownTypeMode.ACW => ManualCommand.MANU_ACW_CHISET,
          BreakdownTypeMode.DCW => ManualCommand.MANU_DCW_CHISET,
          _ => throw new NotSupportedException($"Тип команды {typeCommand} не поддерживается в {nameof(SetHighCurrentLimitAsync)}"),
        };

        string command = $"{GetCommandSyntax(manualCommand)} {value:F3}".Replace(',', '.');
        await breakDown.DeviceProtocol.QueryAsync(command);
        await Task.Delay(delay);

        var actual = await GetHighCurrentLimitAsync(breakDown, typeCommand, delay);
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetHighCurrentLimitAsync)}: Верхний предел тока успешно установлен.", isDeviceLog: true);
          return (true, string.Empty);
        }

        LogWarning($"{nameof(SetHighCurrentLimitAsync)}: Повторная попытка.", isDeviceLog: true);
        await breakDown.DeviceProtocol.QueryAsync(command);
        actual = await GetHighCurrentLimitAsync(breakDown, typeCommand, delay);
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetHighCurrentLimitAsync)}: Верхний предел тока установлен со второй попытки.", isDeviceLog: true);
          return (true, string.Empty);
        }

        string error = $"Не удалось установить верхний предел тока {value:F3} мА. Устройство сообщает: {actual:F3} мА.";
        LogWarning(error, isDeviceLog: true);
        return (false, error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(SetHighCurrentLimitAsync)}", ex, isDeviceLog: true);
        throw;
      }
      finally
      {
        await Task.Delay(delay);
      }
    }


    /// <inheritdoc />
    static public async Task<double> GetHighCurrentLimitAsync(IBreakdownTester breakDown, BreakdownTypeMode typeCommand, int delay)
    {
      LogInformation($"Начало {nameof(GetHighCurrentLimitAsync)}", isDeviceLog: true);

      ManualCommand manualCommand = typeCommand switch
      {
        BreakdownTypeMode.ACW => ManualCommand.MANU_ACW_CHISET,
        BreakdownTypeMode.DCW => ManualCommand.MANU_DCW_CHISET,
        _ => throw new NotSupportedException($"Тип команды {typeCommand} не поддерживается в {nameof(SetHighCurrentLimitAsync)}"),
      };

      try
      {
        if (await ExecutionConfig.GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(GetHighCurrentLimitAsync)}: Устройство в Idle Mode. Возвращаем 0.", isDeviceLog: true);
          return 0;
        }

        await Task.Delay(delay);
        var query = $"{GetCommandSyntax(manualCommand)} ?";
        var response = await breakDown.DeviceProtocol.QueryAsync(query, timeout: 1000);
        LogDebug($"Ответ на {nameof(GetHighCurrentLimitAsync)}: \"{response}\"", isDeviceLog: true);

        if (double.TryParse(response.Replace("mA", "").Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var current))
        {
          LogInformation($"{nameof(GetHighCurrentLimitAsync)}: Результат = {current}", isDeviceLog: true);
          return current;
        }

        LogWarning($"{nameof(GetHighCurrentLimitAsync)}: Не удалось разобрать ток. Возвращаем 0.", isDeviceLog: true);
        return 0;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(GetHighCurrentLimitAsync)}", ex, isDeviceLog: true);
        throw;
      }
    }

    /// <inheritdoc />
    static public async Task<(bool Success, string Message)> SetLowCurrentLimitAsync(IBreakdownTester breakDown, BreakdownTypeMode typeCommand, double value, int delay)
    {
      LogInformation($"Начало {nameof(SetLowCurrentLimitAsync)}: value={value:F3}", isDeviceLog: true);

      try
      {
        if (await ExecutionConfig.GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(SetLowCurrentLimitAsync)}: Устройство в Idle Mode. Пропускаем установку.", isDeviceLog: true);
          return (true, string.Empty);
        }

        ManualCommand manualCommand = typeCommand switch
        {
          BreakdownTypeMode.ACW => ManualCommand.MANU_ACW_CLOSET,
          BreakdownTypeMode.DCW => ManualCommand.MANU_DCW_CLOSET,
          _ => throw new NotSupportedException($"Тип команды {typeCommand} не поддерживается в {nameof(SetLowCurrentLimitAsync)}"),
        };

        string command = $"{GetCommandSyntax(manualCommand)} {value:F3}".Replace(',', '.');
        await breakDown.DeviceProtocol.QueryAsync(command);
        await Task.Delay(delay);

        var actual = await GetLowCurrentLimitAsync(breakDown, typeCommand, delay);
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetLowCurrentLimitAsync)}: Нижний предел тока успешно установлен.", isDeviceLog: true);
          return (true, string.Empty);
        }

        LogWarning($"{nameof(SetLowCurrentLimitAsync)}: Повторная попытка.", isDeviceLog: true);
        await breakDown.DeviceProtocol.QueryAsync(command);
        actual = await GetLowCurrentLimitAsync(breakDown, typeCommand, delay);
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetLowCurrentLimitAsync)}: Нижний предел тока установлен со второй попытки.", isDeviceLog: true);
          return (true, string.Empty);
        }

        string error = $"Не удалось установить нижний предел тока {value:F3} мА. Устройство сообщает: {actual:F3} мА.";
        LogWarning(error, isDeviceLog: true);
        return (false, error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(SetLowCurrentLimitAsync)}", ex, isDeviceLog: true);
        throw;
      }
      finally
      {
        await Task.Delay(delay);
      }
    }


    /// <inheritdoc />
    static public async Task<double> GetLowCurrentLimitAsync(IBreakdownTester breakDown, BreakdownTypeMode typeCommand, int delay)
    {
      LogInformation($"Начало {nameof(GetLowCurrentLimitAsync)}", isDeviceLog: true);

      try
      {
        if (await ExecutionConfig.GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(GetLowCurrentLimitAsync)}: Устройство в Idle Mode. Возвращаем 0.", isDeviceLog: true);
          return 0;
        }

        ManualCommand manualCommand = typeCommand switch
        {
          BreakdownTypeMode.ACW => ManualCommand.MANU_ACW_CLOSET,
          BreakdownTypeMode.DCW => ManualCommand.MANU_DCW_CLOSET,
          _ => throw new NotSupportedException($"Тип команды {typeCommand} не поддерживается в {nameof(SetLowCurrentLimitAsync)}"),
        };

        await Task.Delay(delay);
        var query = $"{GetCommandSyntax(manualCommand)} ?";
        var response = await breakDown.DeviceProtocol.QueryAsync(query, timeout: 1000);
        LogDebug($"Ответ на {nameof(GetLowCurrentLimitAsync)}: \"{response}\"", isDeviceLog: true);

        if (double.TryParse(response.Replace("mA", "").Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var current))
        {
          LogInformation($"{nameof(GetLowCurrentLimitAsync)}: Результат = {current}", isDeviceLog: true);
          return current;
        }

        LogWarning($"{nameof(GetLowCurrentLimitAsync)}: Не удалось разобрать ток. Возвращаем 0.", isDeviceLog: true);
        return 0;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(GetLowCurrentLimitAsync)}", ex, isDeviceLog: true);
        throw;
      }
    }
  }
}
