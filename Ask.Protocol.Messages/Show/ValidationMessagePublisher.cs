using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.IO;
using System.Runtime.CompilerServices;

namespace Ask.Protocol.Messages.Show;

/// <summary>
/// Записывает сообщения проверки данных и конфигурации в журнал и передаёт их в экранный протокол.
/// </summary>
internal static class ValidationMessagePublisher
{
  /// <summary>
  /// Передаёт сообщение проверки данных в экранный протокол с данными исходного места вызова.
  /// </summary>
  /// <param name="message">Сообщение об ошибке проверяемых данных.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя исходного метода.</param>
  /// <param name="callerFile">Путь к исходному файлу.</param>
  /// <param name="callerLine">Номер строки исходного вызова.</param>
  /// <param name="isBlockStart">Признак начала логического блока.</param>
  /// <param name="skipStepModeCheck">Признак вывода без проверки пошагового режима.</param>
  /// <param name="publisherFile">Путь к файлу издателя.</param>
  /// <param name="publisherLine">Номер строки вызова внутри издателя.</param>
  /// <returns>Задача, представляющая вывод сообщения.</returns>
  internal static Task PublishAsync(
    ShowMessageModel message,
    IMessageOutputService outputService,
    string callerName,
    string callerFile,
    int callerLine,
    bool isBlockStart = false,
    bool skipStepModeCheck = false,
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
      SkipStepModeCheck: skipStepModeCheck,
      callerName: displayCallerName,
      callerFile: publisherFile,
      callerLine: publisherLine);
  }
}
