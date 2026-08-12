using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Protocol.Messages.Show;

/// <summary>
/// Передаёт сообщения команд в экранный протокол.
/// </summary>
internal static class CommandMessagePublisher
{
  internal static Task PublishAsync(
    ShowMessageModel message,
    IMessageOutputService outputService,
    bool isBlockStart,
    string callerName,
    string callerFile,
    int callerLine,
    bool skipStepModeCheck = false,
    bool skipPause = false)
  {
    return MessagePublisher.PublishAsync(
      message,
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart,
      skipStepModeCheck,
      skipPause);
  }
}
