using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Protocol.Messages.Show;

/// <summary>
/// Передаёт сообщения о выполнении процессов в экранный протокол.
/// </summary>
internal static class ExecutionMessagePublisher
{
  internal static Task PublishAsync(
    ShowMessageModel message,
    IMessageOutputService? outputService,
    string callerName,
    string callerFile,
    int callerLine,
    bool isBlockStart = false,
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
