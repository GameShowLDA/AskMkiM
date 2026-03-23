using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using static Ask.LogLib.LoggerUtility;
using static NewCore.Function.GPT.Command.FunctionCommandManager;
using static NewCore.Function.GPT.Command.ManualCommandManager;

namespace NewCore.Function.GPT.Helper
{
  internal static class ModeHelper
  {
    static public async Task<(bool Success, string Message)> GetModeAsync(IBreakdownTester breakDown, BreakdownTypeMode typeMode, int delay)
    {
      LogInformation($"Начало выполнения {nameof(GetModeAsync)}", isDeviceLog: true);

      try
      {
        if (ExecutionConfig.GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(GetModeAsync)}: Устройство в Idle Mode. Возвращаем пустую строку.", isDeviceLog: true);
          return (true, string.Empty);
        }

        await Task.Delay(delay);
        var query = $"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} ?";
        var response = await breakDown.DeviceProtocol.QueryAsync(query, timeout: 1000);
        LogDebug($"Ответ на {nameof(GetModeAsync)}: \"{response}\"", isDeviceLog: true);

        var trimmed = response.Trim();
        LogInformation($"{nameof(GetModeAsync)}: Результат = {trimmed}", isDeviceLog: true);
        var result = trimmed.Equals(typeMode.ToString(), StringComparison.OrdinalIgnoreCase);
        return (result, trimmed);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(GetModeAsync)}", ex, isDeviceLog: true);
        throw;
      }
    }
    static public async Task<(bool Success, string Message)> SetModeAsync(IBreakdownTester breakDown, BreakdownTypeMode typeMode, int delay)
    {
      LogInformation($"Начало выполнения {nameof(SetModeAsync)}", isDeviceLog: true);

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        LogInformation($"{nameof(SetModeAsync)}: Устройство в Idle Mode. Пропускаем установку режима.", isDeviceLog: true);
        return (true, string.Empty);
      }

      if (breakDown.Mode == typeMode)
      {
        return (true, string.Empty);
      }

      try
      {
        string expectedMode = typeMode.ToString();

        string command = $"{GetCommandSyntax(ManualCommand.MANU_EDIT_MODE)} {expectedMode}";
        await breakDown.DeviceProtocol.QueryAsync(command);

        await Task.Delay(delay);
        var actualMode = await GetModeAsync(breakDown, typeMode, delay);
        if (actualMode.Success)
        {
          LogInformation($"{nameof(SetModeAsync)}: Режим {expectedMode} успешно установлен с первой попытки.", isDeviceLog: true);
          return (true, string.Empty);
        }

        LogWarning($"{nameof(SetModeAsync)}: Повторная попытка установки режима {expectedMode}.", isDeviceLog: true);
        await breakDown.DeviceProtocol.QueryAsync(command);
        actualMode = await GetModeAsync(breakDown, typeMode, delay);
        if (actualMode.Success)
        {
          LogInformation($"{nameof(SetModeAsync)}: Режим {expectedMode} успешно установлен со второй попытки.", isDeviceLog: true);
          return (true, string.Empty);
        }

        string error = $"Не удалось установить режим {expectedMode}. Устройство сообщает: {actualMode}";
        LogWarning(error, isDeviceLog: true);
        return (false, error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(SetModeAsync)}", ex);
        throw;
      }
    }
  }
}
