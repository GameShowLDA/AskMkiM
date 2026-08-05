using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.IO;
using System.Runtime.CompilerServices;

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
    bool skipPause = false,
    [CallerFilePath] string publisherFile = "",
    [CallerLineNumber] int publisherLine = 0)
  {
    ArgumentNullException.ThrowIfNull(message);

    if (outputService == null)
    {
      return Task.CompletedTask;
    }

    string origin = $"{Path.GetFileName(callerFile)} → {callerName}, строка {callerLine}";
    string displayCallerName = $"{nameof(PublishAsync)} (вызван из {origin})";

    return outputService.ShowMessageAsync(
      message,
      IsBlockStart: isBlockStart,
      SkipStepModeCheck: skipStepModeCheck,
      skipPause: skipPause,
      callerName: displayCallerName,
      callerFile: publisherFile,
      callerLine: publisherLine);
  }
}
