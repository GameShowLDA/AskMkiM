using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using static Ask.LogLib.LoggerUtility;
using System.IO;
using System.Runtime.CompilerServices;

namespace Ask.Protocol.Messages.Show;

/// <summary>
/// Записывает сообщения оборудования в журнал и передаёт их в экранный протокол.
/// </summary>
internal static class EquipmentMessagePublisher
{
  /// <summary>
  /// Записывает сообщение в журнал оборудования и передаёт его указанному сервису вывода.
  /// </summary>
  /// <param name="message">Сообщение оборудования.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="callerName">Имя исходного метода, запросившего публикацию.</param>
  /// <param name="callerFile">Путь к исходному файлу, запросившему публикацию.</param>
  /// <param name="callerLine">Номер строки, запросившей публикацию.</param>
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  /// <exception cref="ArgumentNullException">
  /// Выбрасывается, если <paramref name="message"/> равен <see langword="null"/>.
  /// </exception>
  internal static async Task PublishAsync(
    ShowMessageModel message,
    IMessageOutputService? outputService,
    string callerName,
    string callerFile,
    int callerLine)
  {
    ArgumentNullException.ThrowIfNull(message);
    message.IndentLevel = 1;

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
      await ShowAsync(message, outputService, callerName, callerFile, callerLine);
    }
  }

  /// <summary>
  /// Передаёт сообщение в экранный протокол с указанием publisher и исходного места вызова.
  /// </summary>
  /// <param name="message">Сообщение оборудования.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="originCallerName">Имя исходного метода.</param>
  /// <param name="originCallerFile">Путь к исходному файлу.</param>
  /// <param name="originCallerLine">Номер исходной строки.</param>
  /// <param name="publisherFile">Путь к файлу publisher.</param>
  /// <param name="publisherLine">Номер строки вызова внутри publisher.</param>
  /// <returns>Задача, представляющая операцию вывода сообщения.</returns>
  private static Task ShowAsync(
    ShowMessageModel message,
    IMessageOutputService outputService,
    string originCallerName,
    string originCallerFile,
    int originCallerLine,
    [CallerFilePath] string publisherFile = "",
    [CallerLineNumber] int publisherLine = 0)
  {
    var origin = $"{Path.GetFileName(originCallerFile)} → {originCallerName}, строка {originCallerLine}";
    var displayCallerName = $"{nameof(PublishAsync)} (вызван из {origin})";

    return outputService.ShowMessageAsync(
      message,
      skipPause: true,
      callerName: displayCallerName,
      callerFile: publisherFile,
      callerLine: publisherLine);
  }
}
