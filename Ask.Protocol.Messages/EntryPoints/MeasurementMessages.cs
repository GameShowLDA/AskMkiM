using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Protocol.Messages.Builders;
using Ask.Protocol.Messages.Models;
using Ask.Protocol.Messages.Show;
using System.Runtime.CompilerServices;

namespace Ask.Protocol.Messages.EntryPoints;

/// <summary>
/// Предоставляет единые точки формирования, логирования и вывода сообщений об измерениях.
/// </summary>
public static class MeasurementMessages
{
  /// <summary>
  /// Формирует описание брака для разряда группового метода.
  /// </summary>
  /// <param name="dischargeIndex">Индекс проверяемого разряда.</param>
  /// <param name="bitString">Двоичная маска проверяемого разряда.</param>
  /// <param name="limit">Допустимый предел измеряемой величины.</param>
  /// <param name="result">Измеренное значение.</param>
  /// <param name="unit">Единица измерения.</param>
  /// <param name="limitKind">Положение допустимого предела относительно измеряемого значения.</param>
  /// <returns>Описание результата проверки для итогового заключения.</returns>
  public static string BuildGroupFailure(
    int dischargeIndex,
    string bitString,
    double limit,
    double result,
    Enum unit,
    MeasurementLimitKind limitKind)
    => MeasurementFailureMessageBuilder.BuildGroupFailure(
      dischargeIndex, bitString, limit, result, unit, limitKind);

  /// <summary>
  /// Формирует описание брака для точки узлового метода.
  /// </summary>
  /// <param name="point">Проверяемая точка.</param>
  /// <param name="limit">Допустимый предел измеряемой величины.</param>
  /// <param name="result">Измеренное значение.</param>
  /// <param name="unit">Единица измерения.</param>
  /// <param name="limitKind">Положение допустимого предела относительно измеряемого значения.</param>
  /// <returns>Описание результата проверки для итогового заключения.</returns>
  public static string BuildNodeFailure(
    PointModel point,
    double limit,
    double result,
    Enum unit,
    MeasurementLimitKind limitKind)
    => MeasurementFailureMessageBuilder.BuildNodeFailure(
      point, limit, result, unit, limitKind);

  /// <summary>
  /// Формирует описание брака точки при проверке допустимого диапазона.
  /// </summary>
  /// <param name="point">Проверяемая точка.</param>
  /// <param name="lowerLimit">Нижняя граница допустимого диапазона.</param>
  /// <param name="upperLimit">Верхняя граница допустимого диапазона.</param>
  /// <param name="result">Измеренное значение.</param>
  /// <param name="unit">Единица измерения.</param>
  /// <returns>Описание результата проверки для итогового заключения.</returns>
  public static string BuildNodeRangeFailure(
    PointModel point,
    double lowerLimit,
    double upperLimit,
    double result,
    Enum unit)
    => MeasurementFailureMessageBuilder.BuildNodeRangeFailure(
      point, lowerLimit, upperLimit, result, unit);

  /// <summary>
  /// Выводит ранее сформированное сообщение с результатом измерения.
  /// </summary>
  /// <param name="checkType">Тип выполняемой проверки.</param>
  /// <param name="message">Сформированное сообщение с результатом измерения.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishBuiltMessageAsync(
    CheckType checkType,
    ShowMessageModel message,
    IMessageOutputService outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    ArgumentNullException.ThrowIfNull(message);
    ArgumentNullException.ThrowIfNull(outputService);
    return MeasurementMessagePublisher.PublishAsync(
      message, checkType, outputService, callerName, callerFile, callerLine);
  }

  /// <summary>
  /// Формирует результат повторного измерения неисправной цепи.
  /// </summary>
  /// <param name="measurementTypeCommand">Тип выполненного измерения.</param>
  /// <param name="measurementRange">Измеренное значение и допустимые границы.</param>
  /// <param name="chainDisplay">Обозначение измеренной цепи.</param>
  /// <returns>Результат алгоритма с сообщением об ошибке измерения.</returns>
  public static AlgorithmExecutionResult BuildFaultChainResult(MeasurementTypeCommand measurementTypeCommand, MeasurementRange measurementRange, string chainDisplay)
  {
    var message = BuildMeasurementResultMessage(measurementTypeCommand, measurementRange, chainDisplay);
    message.Status = ShowMessageModel.MessageType.Error;
    message.IndentLevel = 3;
    return AlgorithmExecutionResult.FromErrors(new List<ShowMessageModel> { message });
  }
  /// <summary>
  /// Публикует заголовок начала измерения.
  /// </summary>
  /// <param name="checkType">Тип выполняемой проверки.</param>
  /// <param name="measurementTypeCommand">Тип выполняемого измерения.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="isBlockStart">Признак начала логического блока.</param>
  /// <param name="indentLevel">Уровень отступа сообщения.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  public static Task PublishStartAsync(
    CheckType checkType,
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
      checkType,
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart: isBlockStart,
      skipPause: false);
  }

  /// <summary>
  /// Публикует заголовок измерения тока утечки в режиме проверки прочности изоляции.
  /// </summary>
  /// <param name="checkType">Тип выполняемой проверки.</param>
  /// <param name="measurementTypeCommand">Режим проверки прочности изоляции.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="indentLevel">Уровень отступа сообщения.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishLeakageCurrentStartAsync(
    CheckType checkType,
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
      message, checkType, outputService, callerName, callerFile, callerLine, skipPause: false);
  }

  /// <summary>
  /// Публикует заголовок этапа выполнения измерений.
  /// </summary>
  /// <param name="checkType">Тип выполняемой проверки.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="isBlockStart">Признак начала логического блока.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishMeasurementStageAsync(
    CheckType checkType,
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
      checkType,
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart: isBlockStart,
      skipPause: false);
  }

  /// <summary>
  /// Публикует эталонное измеренное значение.
  /// </summary>
  /// <param name="checkType">Тип выполняемой проверки.</param>
  /// <param name="measurementUnit">Единица измерения эталонного значения.</param>
  /// <param name="value">Измеренное эталонное значение.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishReferenceValueAsync(
    CheckType checkType,
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
      checkType,
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
  /// <param name="checkType">Тип выполняемой проверки.</param>
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
    CheckType checkType,
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
      checkType,
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
  /// <param name="checkType">Тип выполняемой проверки.</param>
  /// <param name="measurementTarget">Обозначение проверяемой точки.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="details">Описание ошибки подключения.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishPointConnectionErrorAsync(
    CheckType checkType,
    string measurementTarget,
    IMessageOutputService outputService,
    string details = "Rизм = Нет подлючения точки",
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return MeasurementMessagePublisher.PublishAsync(
      MeasurementMessageBuilder.BuildPointConnectionError(measurementTarget, details),
      checkType,
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
  /// <param name="checkType">Тип выполняемой проверки.</param>
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
    CheckType checkType,
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
      checkType,
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
  /// <param name="checkType">Тип выполняемой проверки.</param>
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
    CheckType checkType,
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
      checkType,
      outputService,
      callerName,
      callerFile,
      callerLine,
      isVisible: DeviceDisplayConfig.GetMeasurementResultsVisibility());
  }

  /// <summary>
  /// Публикует погрешность измерения.
  /// </summary>
  /// <param name="checkType">Тип выполняемой проверки.</param>
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
    CheckType checkType,
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
      checkType,
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Публикует промежуточный результат измерения цепи.
  /// </summary>
  /// <param name="checkType">Тип выполняемой проверки.</param>
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
    CheckType checkType,
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
      checkType,
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
    CheckType checkType,
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
      checkType,
      outputService,
      callerName,
      callerFile,
      callerLine,
      isVisible: isVisible);
  }
}
