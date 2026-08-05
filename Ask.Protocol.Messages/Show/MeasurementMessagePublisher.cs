using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.IO;
using System.Runtime.CompilerServices;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Protocol.Messages.Show;

/// <summary>
/// Записывает сообщения об измерениях в журнал и передаёт их в экранный протокол.
/// </summary>
internal static class MeasurementMessagePublisher
{
  /// <summary>
  /// Записывает сообщение об измерении в журнал оборудования и передаёт его сервису вывода.
  /// </summary>
  /// <param name="message">Сообщение об измерении.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="callerName">Имя исходного метода, запросившего публикацию.</param>
  /// <param name="callerFile">Путь к исходному файлу, запросившему публикацию.</param>
  /// <param name="callerLine">Номер строки, запросившей публикацию.</param>
  /// <param name="isBlockStart">Признак начала логического блока.</param>
  /// <param name="skipPause">Признак пропуска автоматической паузы перед выводом.</param>
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  internal static async Task PublishAsync(
    ShowMessageModel message,
    IMessageOutputService? outputService,
    string callerName,
    string callerFile,
    int callerLine,
    bool isBlockStart = false,
    bool skipPause = true)
  {
    ArgumentNullException.ThrowIfNull(message);

    if (message.Status == ShowMessageModel.MessageType.Error)
    {
      LogError(message.ToString(), isDeviceLog: true);
    }
    else
    {
      LogInformation(message.ToString(), isDeviceLog: true);
    }

    if (outputService != null)
    {
      await ShowAsync(
        message,
        outputService,
        callerName,
        callerFile,
        callerLine,
        isBlockStart,
        skipPause);
    }
  }

  /// <summary>
  /// Передаёт сообщение в экранный протокол с указанием publisher и исходного места вызова.
  /// </summary>
  /// <param name="message">Сообщение об измерении.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="originCallerName">Имя исходного метода.</param>
  /// <param name="originCallerFile">Путь к исходному файлу.</param>
  /// <param name="originCallerLine">Номер исходной строки.</param>
  /// <param name="isBlockStart">Признак начала логического блока.</param>
  /// <param name="skipPause">Признак пропуска автоматической паузы перед выводом.</param>
  /// <param name="publisherFile">Путь к файлу publisher.</param>
  /// <param name="publisherLine">Номер строки вызова внутри publisher.</param>
  /// <returns>Задача, представляющая операцию вывода сообщения.</returns>
  private static Task ShowAsync(
    ShowMessageModel message,
    IMessageOutputService outputService,
    string originCallerName,
    string originCallerFile,
    int originCallerLine,
    bool isBlockStart,
    bool skipPause,
    [CallerFilePath] string publisherFile = "",
    [CallerLineNumber] int publisherLine = 0)
  {
    string origin = $"{Path.GetFileName(originCallerFile)} → {originCallerName}, строка {originCallerLine}";
    string displayCallerName = $"{nameof(PublishAsync)} (вызван из {origin})";

    return outputService.ShowMessageAsync(
      message,
      IsBlockStart: isBlockStart,
      skipPause: skipPause,
      callerName: displayCallerName,
      callerFile: publisherFile,
      callerLine: publisherLine);
  }
}
