using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System.Globalization;
using static Ask.LogLib.LoggerUtility;
using static NewCore.Function.GPT.Command.FunctionCommandManager;
using static NewCore.Function.GPT.Command.ManualCommandManager;

namespace NewCore.Function.GPT.Helper
{
  static internal class ArcCurrentHelper
  {
    /// <inheritdoc />
    static public async Task<(bool Success, string Message)> SetArcCurrentAsync(IBreakdownTester breakDown, BreakdownTypeMode typeCommand, double value, int delay)
    {
      LogInformation($"Начало {nameof(SetArcCurrentAsync)}: value={value:F3}", isDeviceLog: true);

      try
      {
        if (await ExecutionConfig.GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(SetArcCurrentAsync)}: Устройство в Idle Mode. Пропускаем установку.", isDeviceLog: true);
          return (true, string.Empty);
        }

        ManualCommand manualCommand = typeCommand switch
        {
          BreakdownTypeMode.ACW => ManualCommand.MANU_ACW_ARCCURRENT,
          BreakdownTypeMode.DCW => ManualCommand.MANU_DCW_ARCCURRENT,
          _ => throw new NotSupportedException($"Тип команды {typeCommand} не поддерживается в {nameof(SetArcCurrentAsync)}"),
        };

        string command = $"{GetCommandSyntax(manualCommand)} {value:F3}".Replace(',', '.');

        await breakDown.DeviceProtocol.QueryAsync(command);
        await Task.Delay(delay);

        var actual = await GetArcCurrentAsync(breakDown, typeCommand, delay);
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetArcCurrentAsync)}: Дуговой ток успешно установлен.", isDeviceLog: true);
          return (true, string.Empty);
        }

        LogWarning($"{nameof(SetArcCurrentAsync)}: Повторная попытка.", isDeviceLog: true);
        await breakDown.DeviceProtocol.QueryAsync(command);
        actual = await GetArcCurrentAsync(breakDown, typeCommand, delay);
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetArcCurrentAsync)}: Дуговой ток установлен со второй попытки.", isDeviceLog: true);
          return (true, string.Empty);
        }

        string error = $"Не удалось установить ток дуги {value:F3} мА. Устройство сообщает: {actual:F3} мА.";
        LogWarning(error, isDeviceLog: true);
        return (false, error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(SetArcCurrentAsync)}", ex, isDeviceLog: true);
        throw;
      }
      finally
      {
        await Task.Delay(delay);
      }
    }

    /// <inheritdoc />
    static public async Task<double> GetArcCurrentAsync(IBreakdownTester breakDown, BreakdownTypeMode typeCommand, int delay)
    {
      LogInformation($"Начало {nameof(GetArcCurrentAsync)}", isDeviceLog: true);

      try
      {
        if (await ExecutionConfig.GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(GetArcCurrentAsync)}: Устройство в Idle Mode. Возвращаем 0.", isDeviceLog: true);
          return 0;
        }

        ManualCommand manualCommand = typeCommand switch
        {
          BreakdownTypeMode.ACW => ManualCommand.MANU_ACW_ARCCURRENT,
          BreakdownTypeMode.DCW => ManualCommand.MANU_DCW_ARCCURRENT,
          _ => throw new NotSupportedException($"Тип команды {typeCommand} не поддерживается в {nameof(SetArcCurrentAsync)}"),
        };

        var query = $"{GetCommandSyntax(manualCommand)} ?";
        var response = await breakDown.DeviceProtocol.QueryAsync(query, timeout: 1000);
        LogDebug($"Ответ на {nameof(GetArcCurrentAsync)}: \"{response}\"", isDeviceLog: true);

        if (double.TryParse(response.Replace("mA", "").Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var arc))
        {
          LogInformation($"{nameof(GetArcCurrentAsync)}: Результат = {arc}", isDeviceLog: true);
          return arc;
        }

        LogWarning($"{nameof(GetArcCurrentAsync)}: Не удалось разобрать ток дуги. Возвращаем 0.", isDeviceLog: true);
        return 0;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(GetArcCurrentAsync)}", ex, isDeviceLog: true);
        throw;
      }
    }
  }
}
