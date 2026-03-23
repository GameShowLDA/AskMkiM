using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System.Globalization;
using static Ask.LogLib.LoggerUtility;
using static NewCore.Function.GPT.Command.FunctionCommandManager;
using static NewCore.Function.GPT.Command.ManualCommandManager;


namespace NewCore.Function.GPT.Helper
{
  static internal class OffsetHelper
  {
    static public async Task<(bool Success, string Message)> SetOffsetAsync(IBreakdownTester breakDown, BreakdownTypeMode typeCommand, double value, int delay)
    {
      LogInformation($"Начало {nameof(SetOffsetAsync)}: value={value:F3}", isDeviceLog: true);

      try
      {
        if (ExecutionConfig.GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(SetOffsetAsync)}: Устройство в Idle Mode. Пропускаем установку.", isDeviceLog: true);
          return (true, string.Empty);
        }

        ManualCommand manualCommand = typeCommand switch
        {
          BreakdownTypeMode.ACW => ManualCommand.MANU_ACW_REF,
          BreakdownTypeMode.DCW => ManualCommand.MANU_DCW_REF,
          BreakdownTypeMode.IR => ManualCommand.MANU_IR_REF,
          _ => throw new NotSupportedException($"Тип команды {typeCommand} не поддерживается в {nameof(SetOffsetAsync)}"),
        };

        string command = $"{GetCommandSyntax(manualCommand)} {value:F3}".Replace(',', '.');
        await breakDown.DeviceProtocol.QueryAsync(command);
        await Task.Delay(delay);

        var actual = await GetOffsetAsync(breakDown, typeCommand, delay);
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetOffsetAsync)}: Смещение успешно установлено.", isDeviceLog: true);
          return (true, string.Empty);
        }

        LogWarning($"{nameof(SetOffsetAsync)}: Повторная попытка.", isDeviceLog: true);
        await breakDown.DeviceProtocol.QueryAsync(command);
        actual = await GetOffsetAsync(breakDown, typeCommand, delay);
        if (Math.Abs(actual - value) < 0.1)
        {
          LogInformation($"{nameof(SetOffsetAsync)}: Смещение установлено со второй попытки.", isDeviceLog: true);
          return (true, string.Empty);
        }

        string error = $"Не удалось установить смещение {value:F3} мА. Устройство сообщает: {actual:F3} мА.";
        LogWarning(error, isDeviceLog: true);
        return (false, error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(SetOffsetAsync)}", ex, isDeviceLog: true);
        throw;
      }
      finally
      {
        await Task.Delay(delay);
      }
    }

    /// <inheritdoc />
    static public async Task<double> GetOffsetAsync(IBreakdownTester breakDown, BreakdownTypeMode typeCommand, int delay)
    {
      LogInformation($"Начало {nameof(GetOffsetAsync)}", isDeviceLog: true);

      try
      {
        if (ExecutionConfig.GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(GetOffsetAsync)}: Устройство в Idle Mode. Возвращаем 0.", isDeviceLog: true);
          return 0;
        }

        ManualCommand manualCommand = typeCommand switch
        {
          BreakdownTypeMode.ACW => ManualCommand.MANU_ACW_REF,
          BreakdownTypeMode.DCW => ManualCommand.MANU_DCW_REF,
          BreakdownTypeMode.IR => ManualCommand.MANU_IR_REF,
          _ => throw new NotSupportedException($"Тип команды {typeCommand} не поддерживается в {nameof(SetOffsetAsync)}"),
        };


        var query = $"{GetCommandSyntax(manualCommand)} ?";
        var response = await breakDown.DeviceProtocol.QueryAsync(query, timeout: 1000);
        LogDebug($"Ответ на {nameof(GetOffsetAsync)}: \"{response}\"", isDeviceLog: true);

        if (double.TryParse(response.Replace("mA", "").Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var offset))
        {
          LogInformation($"{nameof(GetOffsetAsync)}: Результат = {offset}", isDeviceLog: true);
          return offset;
        }

        LogWarning($"{nameof(GetOffsetAsync)}: Не удалось разобрать смещение. Возвращаем 0.", isDeviceLog: true);
        return 0;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(GetOffsetAsync)}", ex, isDeviceLog: true);
        throw;
      }
    }
  }
}
