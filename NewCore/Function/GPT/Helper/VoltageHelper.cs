using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System.Globalization;
using static Ask.LogLib.LoggerUtility;
using static NewCore.Function.GPT.Command.FunctionCommandManager;
using static NewCore.Function.GPT.Command.ManualCommandManager;

namespace NewCore.Function.GPT.Helper
{
  internal static class VoltageHelper
  {
    static public async Task<(bool Success, string Message)> SetVoltageAsync(IBreakdownTester breakDown, BreakdownTypeMode typeCommand, double value, double kvValue, int delay)
    {
      LogInformation($"Начало {nameof(SetVoltageAsync)}: value={value:F3}", isDeviceLog: true);

      var maxColtage = typeCommand switch
      {
        BreakdownTypeMode.ACW => breakDown.PiMaxVoltage,
        BreakdownTypeMode.DCW => breakDown.PiMaxVoltage,
        BreakdownTypeMode.IR => breakDown.SiMaxVoltage,
        _ => throw new NotSupportedException($"Тип команды {typeCommand} не поддерживается в {nameof(SetVoltageAsync)}"),
      };

      if (value > maxColtage)
      {
        return (false, $"Максимальное напряжение для {breakDown.Name} = {breakDown.PiMaxVoltage}");
      }

      if (await ExecutionConfig.GetIsIdleModeEnabled())
      {
        LogInformation($"{nameof(SetVoltageAsync)}: Устройство в Idle Mode. Пропускаем установку.", isDeviceLog: true);
        return (true, string.Empty);
      }

      ManualCommand manualCommand = typeCommand switch
      {
        BreakdownTypeMode.ACW => ManualCommand.MANU_ACW_VOLTAGE,
        BreakdownTypeMode.DCW => ManualCommand.MANU_DCW_VOLTAGE,
        BreakdownTypeMode.IR => ManualCommand.MANU_IR_VOLTAGE,
        _ => throw new NotSupportedException($"Тип команды {typeCommand} не поддерживается в {nameof(SetVoltageAsync)}"),
      };

      try
      {
        string command = $"{GetCommandSyntax(manualCommand)} {kvValue:F3}".Replace(',', '.');

        await breakDown.DeviceProtocol.QueryAsync(command);
        await Task.Delay(delay);

        var actualKv = await GetVoltageAsync(breakDown, typeCommand, delay);
        if (Math.Abs(actualKv - kvValue) < 0.01)
        {
          LogInformation($"{nameof(SetVoltageAsync)}: Напряжение {manualCommand.ToString()} установлено успешно.", isDeviceLog: true);
          return (true, string.Empty);
        }

        LogWarning($"{nameof(SetVoltageAsync)}: Повторная попытка.", isDeviceLog: true);
        await breakDown.DeviceProtocol.QueryAsync(command);
        actualKv = await GetVoltageAsync(breakDown, typeCommand, delay);
        if (Math.Abs(actualKv - kvValue) < 0.01)
        {
          LogInformation($"{nameof(SetVoltageAsync)}: Напряжение {manualCommand.ToString()} установлено успешно (со второй попытки).", isDeviceLog: true);
          return (true, string.Empty);
        }

        string error = $"Не удалось установить напряжение {manualCommand.ToString()} {kvValue:F3} кВ. Устройство сообщает: {actualKv:F3} кВ.";
        LogWarning(error, isDeviceLog: true);
        return (false, error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(SetVoltageAsync)}", ex, isDeviceLog: true);
        throw;
      }
      finally
      {
        await Task.Delay(delay);
      }
    }
    static public async Task<double> GetVoltageAsync(IBreakdownTester breakDown, BreakdownTypeMode typeCommand, int delay)
    {
      LogInformation($"Начало {nameof(GetVoltageAsync)}", isDeviceLog: true);

      ManualCommand manualCommand = typeCommand switch
      {
        BreakdownTypeMode.ACW => ManualCommand.MANU_ACW_VOLTAGE,
        BreakdownTypeMode.DCW => ManualCommand.MANU_DCW_VOLTAGE,
        BreakdownTypeMode.IR => ManualCommand.MANU_IR_VOLTAGE,
        _ => throw new NotSupportedException($"Тип команды {typeCommand} не поддерживается в {nameof(SetVoltageAsync)}"),
      };

      try
      {
        if (await ExecutionConfig.GetIsIdleModeEnabled())
        {
          LogInformation($"{nameof(GetVoltageAsync)}: Устройство в Idle Mode. Возвращаем 0.", isDeviceLog: true);
          return 0;
        }

        await Task.Delay(delay);
        var query = $"{GetCommandSyntax(manualCommand)} ?";
        var response = await breakDown.DeviceProtocol.QueryAsync(query, timeout: 1000);
        LogDebug($"Ответ на {nameof(GetVoltageAsync)}: \"{response}\"", isDeviceLog: true);

        if (double.TryParse(response.Replace("kV", "").Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var voltage))
        {
          LogInformation($"{nameof(GetVoltageAsync)}: Результат = {voltage}", isDeviceLog: true);
          return voltage;
        }

        LogWarning($"{nameof(GetVoltageAsync)}: Не удалось разобрать напряжение. Возвращаем 0.", isDeviceLog: true);
        return 0;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(GetVoltageAsync)}", ex, isDeviceLog: true);
        throw;
      }
    }
  }
}
