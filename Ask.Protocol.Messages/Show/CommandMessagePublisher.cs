using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.IO;
using System.Runtime.CompilerServices;

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
    [CallerFilePath] string publisherFile = "",
    [CallerLineNumber] int publisherLine = 0)
  {
    ArgumentNullException.ThrowIfNull(message);
    ArgumentNullException.ThrowIfNull(outputService);

    string origin = $"{Path.GetFileName(callerFile)} → {callerName}, строка {callerLine}";
    string displayCallerName = $"{nameof(PublishAsync)} (вызван из {origin})";

    return outputService.ShowMessageAsync(
      message,
      IsBlockStart: isBlockStart,
      callerName: displayCallerName,
      callerFile: publisherFile,
      callerLine: publisherLine);
  }
}
