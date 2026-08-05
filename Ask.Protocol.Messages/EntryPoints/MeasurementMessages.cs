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
  /// Выводит ранее сформированное сообщение с результатом измерения.
  /// </summary>
  /// <param name="message">Сформированное сообщение с результатом измерения.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishBuiltMessageAsync(
    ShowMessageModel message,
    IMessageOutputService outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    ArgumentNullException.ThrowIfNull(message);
    ArgumentNullException.ThrowIfNull(outputService);
    return MeasurementMessagePublisher.PublishAsync(
      message, outputService, callerName, callerFile, callerLine);
  }
  /// <summary>
  /// Публикует заголовок начала измерения.
  /// </summary>
  /// <param name="measurementTypeCommand">Тип выполняемого измерения.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="isBlockStart">Признак начала логического блока.</param>
  /// <param name="indentLevel">Уровень отступа сообщения.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  public static Task PublishStartAsync(
    MeasurementTypeCommand measurementTypeCommand,
    IMessageOutputService? outputService,
    bool isBlockStart = false,
    int indentLevel = 0,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (outputService == null)
    {
      return Task.CompletedTask;
    }

    ShowMessageModel message = MeasurementMessageBuilder.BuildStart(measurementTypeCommand);
    message.IndentLevel = indentLevel;

    return MeasurementMessagePublisher.PublishAsync(
      message,
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart,
      skipPause: false);
  }

  /// <summary>
  /// Публикует заголовок измерения тока утечки в режиме проверки прочности изоляции.
  /// </summary>
  /// <param name="measurementTypeCommand">Режим проверки прочности изоляции.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="indentLevel">Уровень отступа сообщения.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishLeakageCurrentStartAsync(
    MeasurementTypeCommand measurementTypeCommand,
    IMessageOutputService? outputService,
    int indentLevel = 0,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (outputService == null)
    {
      return Task.CompletedTask;
    }

    ShowMessageModel message = MeasurementMessageBuilder.BuildLeakageCurrentStart(measurementTypeCommand);
    message.IndentLevel = indentLevel;
    return MeasurementMessagePublisher.PublishAsync(
      message, outputService, callerName, callerFile, callerLine, skipPause: false);
  }

  /// <summary>
  /// Публикует заголовок этапа выполнения измерений.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="isBlockStart">Признак начала логического блока.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishMeasurementStageAsync(
    IMessageOutputService? outputService,
    bool isBlockStart = true,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (outputService == null)
    {
      return Task.CompletedTask;
    }

    return MeasurementMessagePublisher.PublishAsync(
      MeasurementMessageBuilder.BuildMeasurementStage(),
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart,
      skipPause: false);
  }

  /// <summary>
  /// Публикует эталонное измеренное значение.
  /// </summary>
  /// <param name="measurementUnit">Единица измерения эталонного значения.</param>
  /// <param name="value">Измеренное эталонное значение.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishReferenceValueAsync(
    Enum measurementUnit,
    double value,
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (outputService == null)
    {
      return Task.CompletedTask;
    }

    return MeasurementMessagePublisher.PublishAsync(
      MeasurementMessageBuilder.BuildReferenceValue(measurementUnit, value),
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Формирует сообщение о неуспешном измерении разряда и переходе к методу полного узла.
  /// </summary>
  /// <param name="measurementTypeCommand">Тип выполненного измерения.</param>
  /// <param name="measurementRange">Измеренное значение и границы допустимого диапазона.</param>
  /// <param name="dischargeNumber">Порядковый номер проверяемого разряда.</param>
  /// <param name="dischargeView">Двоичное представление проверяемого разряда.</param>
  /// <returns>Сообщение о неуспешном измерении и смене алгоритма проверки.</returns>
  public static ShowMessageModel BuildFullNodeFallbackResult(
    MeasurementTypeCommand measurementTypeCommand,
    MeasurementRange measurementRange,
    int dischargeNumber,
    string dischargeView)
  {
    return MeasurementMessageBuilder.BuildFullNodeFallbackResult(
      measurementTypeCommand,
      measurementRange,
      dischargeNumber,
      dischargeView);
  }

  /// <summary>
  /// Публикует заданное испытательное напряжение режима проверки прочности изоляции.
  /// </summary>
  /// <param name="measurementTypeCommand">Режим испытательного напряжения.</param>
  /// <param name="voltage">Заданное испытательное напряжение.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="indentLevel">Уровень отступа сообщения.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Выбрасывается, если <paramref name="measurementTypeCommand"/> не относится
  /// к режиму прочности изоляции ACW или DCW.
  /// </exception>
  public static Task PublishTestVoltageOutputAsync(
    MeasurementTypeCommand measurementTypeCommand,
    double voltage,
    IMessageOutputService? outputService,
    int indentLevel = 0,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (outputService == null)
    {
      return Task.CompletedTask;
    }

    ShowMessageModel message = MeasurementMessageBuilder.BuildTestVoltageOutput(
      measurementTypeCommand,
      voltage);
    message.IndentLevel = indentLevel;

    return MeasurementMessagePublisher.PublishAsync(
      message,
      outputService,
      callerName,
      callerFile,
      callerLine,
      skipPause: false);
  }

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
  /// Формирует сообщение об отсутствии подключения проверяемой точки.
  /// </summary>
  /// <param name="measurementTarget">Обозначение проверяемой точки.</param>
  /// <param name="details">Описание ошибки подключения.</param>
  /// <returns>Модель ошибки подключения точки.</returns>
  public static ShowMessageModel BuildPointConnectionError(
    string measurementTarget,
    string details = "Rизм = Нет подлючения точки")
    => MeasurementMessageBuilder.BuildPointConnectionError(measurementTarget, details);

  /// <summary>
  /// Публикует сообщение об отсутствии подключения проверяемой точки.
  /// </summary>
  /// <param name="measurementTarget">Обозначение проверяемой точки.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="details">Описание ошибки подключения.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishPointConnectionErrorAsync(
    string measurementTarget,
    IMessageOutputService outputService,
    string details = "Rизм = Нет подлючения точки",
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return MeasurementMessagePublisher.PublishAsync(
      MeasurementMessageBuilder.BuildPointConnectionError(measurementTarget, details),
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Формирует результат измерения с явно заданной единицей, состоянием и отступом.
  /// </summary>
  /// <param name="measurementUnit">Единица измерения.</param>
  /// <param name="measurementRange">Измеренное значение и допустимые границы.</param>
  /// <param name="isSuccessful">Признак соответствия допустимому диапазону.</param>
  /// <param name="measurementTarget">Обозначение измеряемой цепи или точки.</param>
  /// <param name="indentLevel">Уровень отступа сообщения.</param>
  /// <returns>Модель результата измерения.</returns>
  public static ShowMessageModel BuildMeasurementResultMessage(
    Enum measurementUnit,
    MeasurementRange measurementRange,
    bool isSuccessful,
    string? measurementTarget,
    int indentLevel = 0)
  {
    var message = MeasurementMessageBuilder.BuildResult(
      measurementUnit,
      measurementRange,
      measurementTarget);
    message.Status = isSuccessful
      ? ShowMessageModel.MessageType.Success
      : ShowMessageModel.MessageType.Error;
    message.IndentLevel = indentLevel;
    return message;
  }

  /// <summary>
  /// Формирует модель результата измерения с заданным состоянием и уровнем отступа.
  /// </summary>
  /// <param name="measurementTypeCommand">Тип выполненного измерения.</param>
  /// <param name="measurementRange">Измеренное значение и границы допустимого диапазона.</param>
  /// <param name="isSuccessful">Признак соответствия результата допустимому диапазону.</param>
  /// <param name="chains">Обозначение измеряемой цепи.</param>
  /// <param name="indentLevel">Уровень отступа сообщения.</param>
  /// <returns>Модель результата измерения.</returns>
  public static ShowMessageModel BuildMeasurementResultMessage(
    MeasurementTypeCommand measurementTypeCommand,
    MeasurementRange measurementRange,
    bool isSuccessful,
    string? chains,
    int indentLevel)
  {
    var message = MeasurementMessageBuilder.BuildResult(
      measurementTypeCommand,
      measurementRange,
      chains);
    message.Status = isSuccessful
      ? ShowMessageModel.MessageType.Success
      : ShowMessageModel.MessageType.Error;
    message.IndentLevel = indentLevel;
    return message;
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
  /// Публикует результат измерения с явно заданной единицей измерения.
  /// </summary>
  /// <param name="measurementUnit">Единица измерения.</param>
  /// <param name="measurementRange">Измеренное значение и границы допустимого диапазона.</param>
  /// <param name="isSuccessful">Признак соответствия результата допустимому диапазону.</param>
  /// <param name="measurementTarget">Обозначение измеряемой точки, цепи или разряда.</param>
  /// <param name="executionErrorMessage">Описание брака для итогового заключения.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="executionError">Признак ошибки выполнения, связанной с сообщением.</param>
  /// <param name="canBeDeleted">Признак возможности удаления сообщения.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  /// <exception cref="ArgumentNullException">
  /// Выбрасывается, если <paramref name="measurementUnit"/> или
  /// <paramref name="measurementRange"/> равен <see langword="null"/>.
  /// </exception>
  public static Task PublishResultAsync(
    Enum measurementUnit,
    MeasurementRange measurementRange,
    bool isSuccessful,
    string? measurementTarget = null,
    string? executionErrorMessage = null,
    IMessageOutputService? outputService = null,
    bool executionError = false,
    bool canBeDeleted = false,
    int indentLevel = 2,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    ArgumentNullException.ThrowIfNull(measurementUnit);
    ArgumentNullException.ThrowIfNull(measurementRange);

    if (outputService == null || (isSuccessful && !DeviceDisplayConfig.GetMeasurementResultsVisibility()))
    {
      return Task.CompletedTask;
    }

    ShowMessageModel message = MeasurementMessageBuilder.BuildResult(
      measurementUnit,
      measurementRange,
      measurementTarget);
    message.Status = isSuccessful
      ? ShowMessageModel.MessageType.Success
      : ShowMessageModel.MessageType.Error;
    message.IndentLevel = indentLevel;
    message.ExecutionErrorMessage = executionErrorMessage;
    message.ExecutionError = executionError;
    message.CanBeDeleted = canBeDeleted;

    return MeasurementMessagePublisher.PublishAsync(
      message,
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Публикует погрешность измерения.
  /// </summary>
  /// <param name="measurementUnit">Единица измерения.</param>
  /// <param name="measurementRange">Погрешность и допустимые границы измерения.</param>
  /// <param name="isSuccessful">Признак соответствия результата допустимому диапазону.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="showAllowedRange">Признак включения допустимого диапазона в заголовок.</param>
  /// <param name="indentLevel">Уровень отступа сообщения.</param>
  /// <param name="executionErrorMessage">Описание брака для итогового заключения.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  /// <exception cref="ArgumentNullException">
  /// Выбрасывается, если <paramref name="measurementUnit"/>,
  /// <paramref name="measurementRange"/> или <paramref name="outputService"/>
  /// равен <see langword="null"/>.
  /// </exception>
  public static Task PublishErrorAsync(
    Enum measurementUnit,
    MeasurementRange measurementRange,
    bool isSuccessful,
    IMessageOutputService outputService,
    bool showAllowedRange = false,
    int indentLevel = 2,
    string? executionErrorMessage = null,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    ArgumentNullException.ThrowIfNull(measurementUnit);
    ArgumentNullException.ThrowIfNull(measurementRange);
    ArgumentNullException.ThrowIfNull(outputService);

    ShowMessageModel message = MeasurementMessageBuilder.BuildError(
      measurementUnit,
      measurementRange,
      showAllowedRange);
    message.Status = isSuccessful
      ? ShowMessageModel.MessageType.Success
      : ShowMessageModel.MessageType.Error;
    message.IndentLevel = indentLevel;
    message.ExecutionErrorMessage = executionErrorMessage;

    return MeasurementMessagePublisher.PublishAsync(
      message,
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
