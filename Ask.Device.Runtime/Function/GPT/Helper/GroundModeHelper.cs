using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using static Ask.LogLib.LoggerUtility;
using static NewCore.Function.GPT.Command.ManualCommandManager;

namespace NewCore.Function.GPT.Helper
{
  internal static class GroundModeHelper
  {
    public static async Task<(bool Success, string Message)> SetGroundModeAsync(IBreakdownTester breakDown, bool state, int delay)
    {
      LogInformation($"Начало {nameof(SetGroundModeAsync)}: state={state}", isDeviceLog: true);

      try
      {
        if (ExecutionConfig.GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(SetGroundModeAsync)}: Устройство в Idle Mode. Пропускаем установку.", isDeviceLog: true);
          return (true, string.Empty);
        }

        var expectedState = state ? "ON" : "OFF";
        var command = $"{GetCommandSyntax(ManualCommand.MANU_UTILITY_GROUNDMODE)} {expectedState}";

        await breakDown.DeviceProtocol.QueryAsync(command);
        await Task.Delay(delay);

        var actual = await GetGroundModeAsync(breakDown, delay);
        if (actual == state)
        {
          LogInformation($"{nameof(SetGroundModeAsync)}: Земля успешно переключена.", isDeviceLog: true);
          return (true, string.Empty);
        }

        LogWarning($"{nameof(SetGroundModeAsync)}: Повторная попытка.", isDeviceLog: true);
        await breakDown.DeviceProtocol.QueryAsync(command);
        actual = await GetGroundModeAsync(breakDown, delay);
        if (actual == state)
        {
          LogInformation($"{nameof(SetGroundModeAsync)}: Земля переключена со второй попытки.", isDeviceLog: true);
          return (true, string.Empty);
        }

        var actualState = actual ? "ON" : "OFF";
        var error = $"Не удалось установить землю в состояние {expectedState}. Устройство сообщает: {actualState}.";
        LogWarning(error, isDeviceLog: true);
        return (false, error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(SetGroundModeAsync)}", ex, isDeviceLog: true);
        throw;
      }
      finally
      {
        await Task.Delay(delay);
      }
    }

    public static async Task<bool> GetGroundModeAsync(IBreakdownTester breakDown, int delay)
    {
      LogInformation($"Начало {nameof(GetGroundModeAsync)}", isDeviceLog: true);

      try
      {
        if (ExecutionConfig.GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(GetGroundModeAsync)}: Устройство в Idle Mode. Возвращаем false.", isDeviceLog: true);
          return false;
        }

        await Task.Delay(delay);
        var query = $"{GetCommandSyntax(ManualCommand.MANU_UTILITY_GROUNDMODE)} ?";
        var response = await breakDown.DeviceProtocol.QueryAsync(query, timeout: 1000);
        LogDebug($"Ответ на {nameof(GetGroundModeAsync)}: \"{response}\"", isDeviceLog: true);

        var trimmed = response.Trim();
        if (trimmed.Equals("ON", StringComparison.OrdinalIgnoreCase))
        {
          LogInformation($"{nameof(GetGroundModeAsync)}: Результат = ON", isDeviceLog: true);
          return true;
        }

        if (trimmed.Equals("OFF", StringComparison.OrdinalIgnoreCase))
        {
          LogInformation($"{nameof(GetGroundModeAsync)}: Результат = OFF", isDeviceLog: true);
          return false;
        }

        LogWarning($"{nameof(GetGroundModeAsync)}: Не удалось разобрать состояние земли. Возвращаем false.", isDeviceLog: true);
        return false;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(GetGroundModeAsync)}", ex, isDeviceLog: true);
        throw;
      }
    }
  }
}
