using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Protocol.Messages.Show;

/// <summary>
/// Передаёт сообщения самоконтроля оборудования в экранный протокол.
/// </summary>
internal static class SelfTestMessagePublisher
{
  internal static Task PublishAsync(
    ShowMessageModel message,
    IMessageOutputService? outputService,
    string callerName,
    string callerFile,
    int callerLine,
    bool isBlockStart = false,
    bool skipStepModeCheck = false,
    bool skipPause = false,
    bool ignoreOutputValidation = false)
    => MessagePublisher.PublishAsync(
      message,
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart,
      skipStepModeCheck,
      skipPause,
      ignoreOutputValidation: ignoreOutputValidation);
}
