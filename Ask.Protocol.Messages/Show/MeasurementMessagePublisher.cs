using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.FileEnums;

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
  /// <param name="checkType">Тип выполняемой проверки.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="callerName">Имя исходного метода, запросившего публикацию.</param>
  /// <param name="callerFile">Путь к исходному файлу, запросившему публикацию.</param>
  /// <param name="callerLine">Номер строки, запросившей публикацию.</param>
  /// <param name="isVisible">Признак отображения сообщения согласно пользовательским настройкам.</param>
  /// <param name="isBlockStart">Признак начала логического блока.</param>
  /// <param name="skipPause">Признак пропуска автоматической паузы перед выводом.</param>
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  internal static Task PublishAsync(
    ShowMessageModel message,
    CheckType checkType,
    IMessageOutputService? outputService,
    string callerName,
    string callerFile,
    int callerLine,
    bool isVisible = true,
    bool isBlockStart = false,
    bool skipPause = true)
  {
    if (outputService == null ||
        (message.Status == ShowMessageModel.MessageType.Success &&
         checkType != CheckType.Metrology &&
         !isVisible))
    {
      return Task.CompletedTask;
    }

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
