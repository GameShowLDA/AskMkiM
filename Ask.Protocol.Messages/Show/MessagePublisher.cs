using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.IO;
using System.Runtime.CompilerServices;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Protocol.Messages.Show;

/// <summary>
/// Передаёт сформированное сообщение в экранный протокол и при необходимости записывает его в журнал устройств.
/// </summary>
internal static class MessagePublisher
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
    bool logToDeviceJournal = false,
    bool ignoreOutputValidation = false,
    [CallerFilePath] string publisherFile = "",
    [CallerLineNumber] int publisherLine = 0)
  {
    ArgumentNullException.ThrowIfNull(message);

    if (logToDeviceJournal)
    {
      LogToDeviceJournal(message);
    }

    if (outputService == null)
    {
      return Task.CompletedTask;
    }

    string origin = $"{Path.GetFileName(callerFile)} → {callerName}, строка {callerLine}";
    string displayCallerName = $"PublishAsync (вызван из {origin})";

    return outputService.ShowMessageAsync(
      message,
      IsBlockStart: isBlockStart,
      SkipStepModeCheck: skipStepModeCheck,
      skipPause: skipPause,
      ignoreOutputValidation: ignoreOutputValidation,
      callerName: displayCallerName,
      callerFile: publisherFile,
      callerLine: publisherLine);
  }

  private static void LogToDeviceJournal(ShowMessageModel message)
  {
    if (message.Status == ShowMessageModel.MessageType.Error)
    {
      LogError(message.ToString(), isDeviceLog: true);
      return;
    }

    LogInformation(message.ToString(), isDeviceLog: true);
  }
}
