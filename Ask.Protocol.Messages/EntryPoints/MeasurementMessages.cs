using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Protocol.Messages.Builders;
using Ask.Protocol.Messages.Show;
using System.Runtime.CompilerServices;

namespace Ask.Protocol.Messages.EntryPoints;

/// <summary>
/// Предоставляет единые точки формирования, логирования и вывода сообщений об измерениях.
/// </summary>
public static class MeasurementMessages
{
  /// <summary>
  /// Формирует сообщение о результате измерения цепи.
  /// </summary>
  /// <param name="measurementTypeCommand">Тип выполненного измерения.</param>
  /// <param name="measurementRange">Измеренное значение и границы допустимого диапазона.</param>
  /// <param name="chains">Обозначение измеряемой цепи.</param>
  /// <param name="comparisonSign">Знак сравнения перед измеренным значением.</param>
  /// <returns>Сообщение о результате измерения цепи.</returns>
  /// <exception cref="ArgumentNullException">
  /// Выбрасывается, если <paramref name="measurementRange"/> равен <see langword="null"/>.
  /// </exception>
  public static ShowMessageModel BuildMeasurementResultMessage(
    MeasurementTypeCommand measurementTypeCommand,
    MeasurementRange measurementRange,
    string? chains = null,
    string comparisonSign = "=")
  {
    return MeasurementMessageBuilder.BuildResult(
      measurementTypeCommand,
      measurementRange,
      chains,
      comparisonSign);
  }

  /// <summary>
  /// Публикует итоговый результат измерения цепи.
  /// </summary>
  /// <param name="measurementTypeCommand">Тип выполненного измерения.</param>
  /// <param name="measurementRange">Измеренное значение и границы допустимого диапазона.</param>
  /// <param name="isSuccessful">Признак соответствия результата допустимому диапазону.</param>
  /// <param name="chains">Обозначение измеряемой цепи.</param>
  /// <param name="comparisonSign">Знак сравнения перед измеренным значением.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  /// <exception cref="ArgumentNullException">
  /// Выбрасывается, если <paramref name="measurementRange"/> равен <see langword="null"/>.
  /// </exception>
  public static Task PublishResultAsync(
    MeasurementTypeCommand measurementTypeCommand,
    MeasurementRange measurementRange,
    bool isSuccessful,
    string? chains = null,
    string comparisonSign = "=",
    IMessageOutputService? outputService = null,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return PublishAsync(
      measurementTypeCommand,
      measurementRange,
      isSuccessful,
      DeviceDisplayConfig.GetMeasurementResultsVisibility(),
      chains,
      comparisonSign,
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Публикует промежуточный результат измерения цепи.
  /// </summary>
  /// <param name="measurementTypeCommand">Тип выполненного измерения.</param>
  /// <param name="measurementRange">Измеренное значение и границы допустимого диапазона.</param>
  /// <param name="isSuccessful">Признак соответствия результата допустимому диапазону.</param>
  /// <param name="chains">Обозначение измеряемой цепи.</param>
  /// <param name="comparisonSign">Знак сравнения перед измеренным значением.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  /// <exception cref="ArgumentNullException">
  /// Выбрасывается, если <paramref name="measurementRange"/> равен <see langword="null"/>.
  /// </exception>
  public static Task PublishIntermediateResultAsync(
    MeasurementTypeCommand measurementTypeCommand,
    MeasurementRange measurementRange,
    bool isSuccessful,
    string? chains = null,
    string comparisonSign = "=",
    IMessageOutputService? outputService = null,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return PublishAsync(
      measurementTypeCommand,
      measurementRange,
      isSuccessful,
      DeviceDisplayConfig.GetIntermediateMeasurementResultsVisibility(),
      chains,
      comparisonSign,
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  private static Task PublishAsync(
    MeasurementTypeCommand measurementTypeCommand,
    MeasurementRange measurementRange,
    bool isSuccessful,
    bool isVisible,
    string? chains,
    string comparisonSign,
    IMessageOutputService? outputService,
    string callerName,
    string callerFile,
    int callerLine)
  {
    ArgumentNullException.ThrowIfNull(measurementRange);

    if (outputService == null || (isSuccessful && !isVisible))
    {
      return Task.CompletedTask;
    }

    ShowMessageModel message = BuildMeasurementResultMessage(
      measurementTypeCommand,
      measurementRange,
      chains,
      comparisonSign);

    message.Status = isSuccessful
      ? ShowMessageModel.MessageType.Success
      : ShowMessageModel.MessageType.Error;
    message.IndentLevel = 2;

    return MeasurementMessagePublisher.PublishAsync(
      message,
      outputService,
      callerName,
      callerFile,
      callerLine);
  }
}
