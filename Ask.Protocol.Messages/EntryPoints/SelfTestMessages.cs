using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Protocol.Messages.Builders;
using Ask.Protocol.Messages.Show;
using System.Runtime.CompilerServices;

namespace Ask.Protocol.Messages.EntryPoints;

/// <summary>
/// Предоставляет единые точки формирования и публикации сообщений самоконтроля оборудования.
/// </summary>
public static class SelfTestMessages
{
  /// <summary>
  /// Публикует информационное сообщение этапа самоконтроля.
  /// </summary>
  /// <param name="header">Заголовок этапа самоконтроля.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="message">Дополнительное описание этапа.</param>
  /// <param name="indentLevel">Уровень отступа сообщения.</param>
  /// <param name="isBlockStart">Признак начала логического блока.</param>
  /// <param name="skipPause">Признак пропуска автоматической паузы.</param>
  /// <param name="ignoreOutputValidation">Признак вывода без проверки его доступности.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishInformationAsync(
    string header,
    IMessageOutputService? outputService,
    string? message = null,
    int indentLevel = 0,
    bool isBlockStart = false,
    bool skipPause = false,
    bool ignoreOutputValidation = false,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
    => SelfTestMessagePublisher.PublishAsync(
      SelfTestMessageBuilder.BuildInformation(header, message, indentLevel),
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart,
      skipPause: skipPause,
      ignoreOutputValidation: ignoreOutputValidation);

  /// <summary>
  /// Публикует сообщение об ошибке самоконтроля.
  /// </summary>
  /// <param name="details">Описание ошибки самоконтроля.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="header">Заголовок сообщения об ошибке.</param>
  /// <param name="indentLevel">Уровень отступа сообщения.</param>
  /// <param name="skipPause">Признак пропуска автоматической паузы.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishErrorAsync(
    string details,
    IMessageOutputService? outputService,
    string header = "Ошибка",
    int indentLevel = 0,
    bool skipPause = false,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
    => SelfTestMessagePublisher.PublishAsync(
      SelfTestMessageBuilder.BuildError(details, header, indentLevel),
      outputService,
      callerName,
      callerFile,
      callerLine,
      skipPause: skipPause);

  /// <summary>
  /// Публикует результат операции самоконтроля.
  /// </summary>
  /// <param name="header">Заголовок проверяемой операции.</param>
  /// <param name="isSuccessful">Признак успешного результата проверки.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="message">Дополнительное описание результата.</param>
  /// <param name="indentLevel">Уровень отступа сообщения.</param>
  /// <param name="executionErrorMessage">Описание ошибки для итогового протокола выполнения.</param>
  /// <param name="executionError">Признак ошибки выполнения.</param>
  /// <param name="canBeDeleted">Признак возможности удалить сообщение из экранного протокола.</param>
  /// <param name="isBlockStart">Признак начала логического блока.</param>
  /// <param name="skipPause">Признак пропуска автоматической паузы.</param>
  /// <param name="isStepModeCheckpoint">Признак контрольной точки пошагового режима.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishResultAsync(
    string header,
    bool isSuccessful,
    IMessageOutputService? outputService,
    string? message = null,
    int indentLevel = 0,
    string? executionErrorMessage = null,
    bool? executionError = null,
    bool? canBeDeleted = null,
    bool isBlockStart = false,
    bool skipPause = true,
    bool isStepModeCheckpoint = false,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
    => SelfTestMessagePublisher.PublishAsync(
      SelfTestMessageBuilder.BuildResult(
        header,
        isSuccessful,
        message,
        indentLevel,
        executionErrorMessage,
        executionError,
        canBeDeleted,
        isStepModeCheckpoint),
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart,
      skipPause: skipPause);

  /// <summary>
  /// Публикует командный заголовок шага самоконтроля.
  /// </summary>
  /// <param name="header">Заголовок шага самоконтроля.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="message">Дополнительное описание шага.</param>
  /// <param name="indentLevel">Уровень отступа сообщения.</param>
  /// <param name="onlyWhenStepMode">Признак вывода только в пошаговом режиме.</param>
  /// <param name="isBlockStart">Признак начала логического блока.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishCommandAsync(
    string header,
    IMessageOutputService? outputService,
    string? message = null,
    int indentLevel = 0,
    bool onlyWhenStepMode = false,
    bool isBlockStart = true,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (onlyWhenStepMode && !StepControlManager.StepMode)
    {
      return Task.CompletedTask;
    }

    return SelfTestMessagePublisher.PublishAsync(
      SelfTestMessageBuilder.BuildCommand(header, message, indentLevel, isBlockStart),
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart);
  }

  /// <summary>
  /// Публикует результат измерительного шага самоконтроля мультиметра.
  /// </summary>
  /// <param name="isSuccessful">Признак соответствия результата допустимому диапазону.</param>
  /// <param name="result">Измеренное значение.</param>
  /// <param name="parameter">Обозначение проверяемого параметра.</param>
  /// <param name="unit">Единица измерения результата.</param>
  /// <param name="idealResult">Эталонное значение результата.</param>
  /// <param name="percentageError">Допустимая относительная погрешность в процентах.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishMultimeterMeasurementResultAsync(
    bool isSuccessful,
    double result,
    string parameter,
    string unit,
    double idealResult,
    int percentageError,
    IMessageOutputService outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
    => SelfTestMessagePublisher.PublishAsync(
      SelfTestMessageBuilder.BuildMultimeterMeasurementResult(
        isSuccessful,
        result,
        parameter,
        unit,
        idealResult,
        percentageError,
        !isSuccessful || DeviceDisplayConfig.GetMeasurementResultsVisibility()),
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart: false);

  /// <summary>
  /// Публикует результат проверки активного сопротивления конденсатора.
  /// </summary>
  /// <param name="result">Измеренное активное сопротивление.</param>
  /// <param name="isSuccessful">Признак успешного результата проверки.</param>
  /// <param name="capacitanceValue">Эталонная ёмкость проверяемого конденсатора.</param>
  /// <param name="minimumResistance">Минимально допустимое активное сопротивление.</param>
  /// <param name="resistanceUnit">Единица измерения сопротивления.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishActiveResistanceResultAsync(
    double result,
    bool isSuccessful,
    string capacitanceValue,
    double minimumResistance,
    string resistanceUnit,
    IMessageOutputService outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (isSuccessful && !DeviceDisplayConfig.GetIntermediateMeasurementResultsVisibility())
    {
      return Task.CompletedTask;
    }

    return SelfTestMessagePublisher.PublishAsync(
      SelfTestMessageBuilder.BuildActiveResistanceResult(
        result,
        isSuccessful,
        capacitanceValue,
        minimumResistance,
        resistanceUnit),
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart: false);
  }
}
