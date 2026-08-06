using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;

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
  internal static Task PublishAsync(
    ShowMessageModel message,
    IMessageOutputService? outputService,
    string callerName,
    string callerFile,
    int callerLine,
    bool isBlockStart = false,
    bool skipPause = true)
  {
    return MessagePublisher.PublishAsync(
      message,
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart: isBlockStart,
      skipPause: skipPause,
      logToDeviceJournal: true);
  }
}
