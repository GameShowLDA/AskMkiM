using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;

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
  internal static Task PublishAsync(
    ShowMessageModel message,
    IMessageOutputService outputService,
    string callerName,
    string callerFile,
    int callerLine)
  {
    return MessagePublisher.PublishAsync(
      message,
      outputService,
      callerName,
      callerFile,
      callerLine,
      skipPause: true,
      logToDeviceJournal: true);
  }
}
