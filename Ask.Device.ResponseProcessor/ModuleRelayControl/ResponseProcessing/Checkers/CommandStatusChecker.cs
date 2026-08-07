using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseModels;

namespace Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing.Checkers;

/// <summary>
/// Проверяет статус выполнения команды МКР.
/// </summary>
internal static class CommandStatusChecker
{
  internal static string? GetError(string response)
  {
    CommandStatusResponse? model = ResponseDeserializer.Deserialize<CommandStatusResponse>(response);
    return model?.Status switch
    {
      string status when status.Equals("UnknownCommand", StringComparison.OrdinalIgnoreCase) =>
        "Неизвестная команда программы.",
      string status when status.Equals("InvalidParametr", StringComparison.OrdinalIgnoreCase) ||
                         status.Equals("InvalidParameter", StringComparison.OrdinalIgnoreCase) =>
        "Неверный параметр команды.",
      _ => null,
    };
  }

  internal static string? GetStatus(string response)
    => ResponseDeserializer.Deserialize<CommandStatusResponse>(response)?.Status;
}
