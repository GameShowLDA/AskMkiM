using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Protocol.Messages.Builders;
using Ask.Protocol.Messages.Show;
using System.Runtime.CompilerServices;

namespace Ask.Protocol.Messages.EntryPoints;

/// <summary>
/// Предоставляет единые точки формирования, логирования и вывода сообщений метрологических режимов.
/// </summary>
public static class MetrologyMessages
{
  /// <summary>
  /// Публикует ошибку расчёта допустимого диапазона метрологического измерения.
  /// </summary>
  /// <param name="details">Описание ошибки расчёта.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию ошибки.</returns>
  public static Task PublishToleranceCalculationErrorAsync(
    string details,
    IMessageOutputService outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return MetrologyMessagePublisher.PublishAsync(
      MetrologyMessageBuilder.BuildToleranceCalculationError(details),
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Публикует сводку предельных погрешностей метрологического режима.
  /// </summary>
  /// <param name="command">Метрологический режим.</param>
  /// <param name="minimumError">Максимальная отрицательная погрешность.</param>
  /// <param name="maximumError">Максимальная положительная погрешность.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сводки.</returns>
  public static async Task PublishResultSummaryAsync(
    MeasurementTypeCommand command,
    double minimumError,
    double maximumError,
    IMessageOutputService outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    await MetrologyMessagePublisher.PublishAsync(
      MetrologyMessageBuilder.BuildResultHeader(command),
      outputService,
      callerName,
      callerFile,
      callerLine);
    await MetrologyMessagePublisher.PublishAsync(
      MetrologyMessageBuilder.BuildExtremeError(command, minimumError, isPositive: false),
      outputService,
      callerName,
      callerFile,
      callerLine);
    await MetrologyMessagePublisher.PublishAsync(
      MetrologyMessageBuilder.BuildExtremeError(command, maximumError, isPositive: true),
      outputService,
      callerName,
      callerFile,
      callerLine);
  }
}
