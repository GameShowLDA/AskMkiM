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
