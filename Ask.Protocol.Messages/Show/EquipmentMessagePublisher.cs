using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;

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
  /// <param name="logToDeviceJournal">Признак записи сообщения в журнал оборудования.</param>
  /// <param name="isBlockStart">Признак начала блока выполнения.</param>
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  /// <exception cref="ArgumentNullException">
  /// Выбрасывается, если <paramref name="message"/> равен <see langword="null"/>.
  /// </exception>
  internal static Task PublishAsync(
    ShowMessageModel message,
    IMessageOutputService? outputService,
    string callerName,
    string callerFile,
    int callerLine,
    bool logToDeviceJournal = true,
    bool isBlockStart = false)
  {
    ArgumentNullException.ThrowIfNull(message);
    message.IndentLevel = 1;
    return MessagePublisher.PublishAsync(
      message,
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart: isBlockStart,
      skipPause: true,
      logToDeviceJournal: logToDeviceJournal);
  }
}
