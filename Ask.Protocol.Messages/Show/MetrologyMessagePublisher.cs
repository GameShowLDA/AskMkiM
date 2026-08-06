using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Protocol.Messages.Show;

/// <summary>
/// Записывает сообщения метрологических режимов в журнал и передаёт их в экранный протокол.
/// </summary>
internal static class MetrologyMessagePublisher
{
  /// <summary>
  /// Передаёт метрологическое сообщение в экранный протокол с данными исходного места вызова.
  /// </summary>
  /// <param name="message">Публикуемое метрологическое сообщение.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя исходного метода.</param>
  /// <param name="callerFile">Путь к исходному файлу.</param>
  /// <param name="callerLine">Номер строки исходного вызова.</param>
  /// <param name="publisherFile">Путь к файлу издателя.</param>
  /// <param name="publisherLine">Номер строки вызова внутри издателя.</param>
  /// <returns>Задача, представляющая вывод сообщения.</returns>
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
      skipPause: true);
  }
}
