using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Protocol.Messages.Builders;
using Ask.Protocol.Messages.Show;
using System.Runtime.CompilerServices;

namespace Ask.Protocol.Messages.EntryPoints;

/// <summary>
/// Предоставляет единые точки формирования, логирования и вывода сообщений проверки данных и конфигурации.
/// </summary>
public static class ValidationMessages
{
  /// <summary>
  /// Публикует сообщение о некорректном номере проверяемого блока.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishInvalidTestedNumberAsync(
    IMessageOutputService outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return PublishAsync(
      ValidationMessageBuilder.BuildInvalidTestedNumber(),
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Публикует сообщение о некорректном номере проверяющего блока.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishInvalidTesterNumberAsync(
    IMessageOutputService outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return PublishAsync(
      ValidationMessageBuilder.BuildInvalidTesterNumber(),
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Публикует сообщение о совпадении номеров проверяемого и проверяющего блоков.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishDuplicateNumbersAsync(
    IMessageOutputService outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return PublishAsync(
      ValidationMessageBuilder.BuildDuplicateNumbers(),
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Публикует сообщение об отсутствующем диапазоне проверки.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishEmptyRangeAsync(
    IMessageOutputService outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return PublishAsync(
      ValidationMessageBuilder.BuildEmptyRange(),
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Публикует сообщение о некорректном диапазоне проверки.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="details">Описание обнаруженной ошибки диапазона.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishInvalidRangeAsync(
    IMessageOutputService outputService,
    string details,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return PublishAsync(
      ValidationMessageBuilder.BuildInvalidRange(details),
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Публикует сообщение об отсутствии измерителя для выполнения самоконтроля.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishMeterUnavailableAsync(
    IMessageOutputService outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return PublishAsync(
      ValidationMessageBuilder.BuildMeterUnavailable(),
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Публикует сообщение об отсутствии выбранного устройства для выполнения самоконтроля.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishDeviceUnavailableAsync(
    IMessageOutputService outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return PublishAsync(
      ValidationMessageBuilder.BuildDeviceUnavailable(),
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Публикует сообщение об ошибке поиска оборудования для проверки.
  /// </summary>
  /// <param name="message">Описание отсутствующего оборудования.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishEquipmentLookupErrorAsync(
    string message,
    IMessageOutputService outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return PublishAsync(
      ValidationMessageBuilder.BuildEquipmentLookupError(message),
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Публикует ошибку конфигурации оборудования без ожидания снятия паузы.
  /// </summary>
  /// <param name="header">Наименование проверяемого оборудования или системной проверки.</param>
  /// <param name="details">Описание ошибки конфигурации.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishEquipmentConfigurationErrorAsync(
    string header,
    string details,
    IMessageOutputService outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return ValidationMessagePublisher.PublishAsync(
      ValidationMessageBuilder.BuildEquipmentConfigurationError(header, details),
      outputService,
      callerName,
      callerFile,
      callerLine,
      skipPause: true);
  }

  /// <summary>
  /// Публикует сообщение об ошибке введённых данных.
  /// </summary>
  /// <param name="details">Описание ошибки введённых данных.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishDataErrorAsync(
    string details,
    IMessageOutputService outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return ValidationMessagePublisher.PublishAsync(
      ValidationMessageBuilder.BuildDataError(details),
      outputService,
      callerName,
      callerFile,
      callerLine,
      skipStepModeCheck: true);
  }

  /// <summary>
  /// Публикует заголовок запуска и введённые пользователем параметры.
  /// </summary>
  /// <param name="executionTitle">Название запускаемой проверки.</param>
  /// <param name="parameters">Названия и отображаемые значения введённых параметров.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="isCommandHeader">Признак командного типа заголовка.</param>
  /// <param name="isBlockStart">Признак начала логического блока.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщений.</returns>
  public static async Task PublishInputParametersAsync(
    string executionTitle,
    IReadOnlyList<(string Header, string Value)> parameters,
    IMessageOutputService outputService,
    bool isCommandHeader,
    bool isBlockStart = true,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    ArgumentNullException.ThrowIfNull(parameters);

    await ValidationMessagePublisher.PublishAsync(
      ValidationMessageBuilder.BuildInputHeader(executionTitle, isCommandHeader),
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart,
      skipStepModeCheck: true);

    foreach ((string header, string value) in parameters)
    {
      await ValidationMessagePublisher.PublishAsync(
        ValidationMessageBuilder.BuildInputParameter(header, value),
        outputService,
        callerName,
        callerFile,
        callerLine,
        skipStepModeCheck: true);
    }
  }

  private static Task PublishAsync(
    ShowMessageModel message,
    IMessageOutputService outputService,
    string callerName,
    string callerFile,
    int callerLine)
  {
    return ValidationMessagePublisher.PublishAsync(
      message,
      outputService,
      callerName,
      callerFile,
      callerLine);
  }
}
