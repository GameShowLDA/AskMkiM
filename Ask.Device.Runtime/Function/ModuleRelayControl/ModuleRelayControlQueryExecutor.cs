using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Device.Emulator;
using Ask.Device.Runtime.Base.DeviceResponses;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.ModuleRelayControl
{
  /// <summary>
  /// Выполняет команды МКР через реальный протокол или эмулятор и записывает обмен в журнал.
  /// </summary>
  internal sealed class ModuleRelayControlQueryExecutor
  {
    private readonly IRelaySwitchModule _module;
    private readonly IDeviceProtocol _protocol;

    public ModuleRelayControlQueryExecutor(IRelaySwitchModule module)
    {
      _module = module;
      _protocol = DeviceProtocolEmulator.CreateModuleRelayControl(module);
    }

    public async Task<string> QueryAsync(
      string command,
      int timeout,
      CancellationToken cancellationToken = default)
    {
      string mode = ExecutionConfig.GetIsIdleModeEnabled()
        ? "Холостой режим"
        : "Реальное обращение";
      string device = $"{_module.Name}({_module.NumberChassis}.{_module.Number})";

      LogInformation($"{mode} | [{device}] Команда МКР: \"{command}\".", isDeviceLog: true);

      string response = await _protocol.QueryAsync(
        command,
        timeout: timeout,
        cancellationToken: cancellationToken);

      LogInformation(
        $"{mode} | [{device}] Ответ МКР на \"{command}\": \"{(string.IsNullOrEmpty(response) ? "<пустой>" : response)}\".",
        isDeviceLog: true);

      ThrowIfFirmwareRejectedCommand(response, command);

      return response;
    }

    private void ThrowIfFirmwareRejectedCommand(string response, string command)
    {
      if (string.IsNullOrWhiteSpace(response))
      {
        return;
      }

      BaseResponse? parsed = BaseResponse.FromJson(response);
      string? error = parsed?.Status switch
      {
        string status when status.Equals("UnknownCommand", StringComparison.OrdinalIgnoreCase) =>
          "Неизвестная команда программы.",
        string status when status.Equals("InvalidParametr", StringComparison.OrdinalIgnoreCase) ||
                           status.Equals("InvalidParameter", StringComparison.OrdinalIgnoreCase) =>
          "Неверный параметр команды.",
        _ => null,
      };

      if (error != null)
      {
        throw new ModuleRelayControlProtocolException(
          $"{_module.Name} {_module.NumberChassis}.{_module.Number}",
          GetOperationName(command),
          error,
          parsed!.Status!);
      }
    }

    private static string GetOperationName(string command)
    {
      string[] parts = command.Split('.');
      if (!int.TryParse(parts.ElementAtOrDefault(0), out int commandNumber))
      {
        return "Выполнение команды";
      }

      int.TryParse(parts.ElementAtOrDefault(1), out int firstParameter);
      int.TryParse(parts.ElementAtOrDefault(2), out int secondParameter);
      int.TryParse(parts.ElementAtOrDefault(3), out int thirdParameter);

      return commandNumber switch
      {
        1 => "Инициализация",
        2 => "Сброс",
        4 => thirdParameter == 2 ? "Отключение шины" : "Подключение шины",
        5 => firstParameter == 2 ? "Отключение измерителя" : "Подключение измерителя",
        6 => "Самоконтроль точки",
        7 => "Проверка измерителя",
        8 or 82 => thirdParameter == 2 ? "Отключение точки" : "Подключение точки",
        9 => secondParameter == 2 ? "Отключение точек от шины" : "Подключение точек к шине",
        10 => "Самоконтроль внешней шины",
        11 => thirdParameter % 10 == 2 ? "Отключение диапазона точек" : "Подключение диапазона точек",
        81 => "Переподключение точки",
        _ => $"Выполнение команды {commandNumber}",
      };
    }
  }
}
