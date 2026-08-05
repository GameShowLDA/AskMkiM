using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.IO;
using System.Runtime.CompilerServices;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Protocol.Messages.Show;

/// <summary>
/// Записывает сообщения о диапазонах в журнал и передаёт их в экранный протокол.
/// </summary>
internal static class RangeMessagePublisher
{
  /// <summary>
  /// Записывает сообщение о диапазоне в журнал и передаёт его сервису вывода.
  /// </summary>
  /// <param name="message">Сообщение о диапазоне.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="callerName">Имя исходного метода, запросившего публикацию.</param>
  /// <param name="callerFile">Путь к исходному файлу, запросившему публикацию.</param>
  /// <param name="callerLine">Номер строки, запросившей публикацию.</param>
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  internal static async Task PublishAsync(
    ShowMessageModel message,
    IMessageOutputService outputService,
    string callerName,
    string callerFile,
    int callerLine)
  {
    ArgumentNullException.ThrowIfNull(message);
    ArgumentNullException.ThrowIfNull(outputService);

    LogInformation(message.ToString(), isDeviceLog: true);
    await ShowAsync(message, outputService, callerName, callerFile, callerLine);
  }

  private static Task ShowAsync(
    ShowMessageModel message,
    IMessageOutputService outputService,
    string originCallerName,
    string originCallerFile,
    int originCallerLine,
    [CallerFilePath] string publisherFile = "",
    [CallerLineNumber] int publisherLine = 0)
  {
    string origin = $"{Path.GetFileName(originCallerFile)} → {originCallerName}, строка {originCallerLine}";
    string displayCallerName = $"{nameof(PublishAsync)} (вызван из {origin})";

    return outputService.ShowMessageAsync(
      message,
      skipPause: true,
      callerName: displayCallerName,
      callerFile: publisherFile,
      callerLine: publisherLine);
  }
}
