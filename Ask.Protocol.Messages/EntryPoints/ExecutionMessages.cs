using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Protocol.Messages.Builders;
using Ask.Protocol.Messages.Show;
using System.Runtime.CompilerServices;

namespace Ask.Protocol.Messages.EntryPoints;

/// <summary>
/// Формирует и выводит сообщения о выполнении процессов.
/// </summary>
public static class ExecutionMessages
{
  /// <summary>
  /// Выводит сообщение о подготовке устройств, если включён вывод параметров выполнения.
  /// </summary>
  public static Task ShowDevicesPreparationAsync(
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (!DeviceDisplayConfig.GetExecutionParametersVisibility())
    {
      return Task.CompletedTask;
    }

    var message = ExecutionMessageBuilder.BuildDevicesPreparationMessage();
    return ExecutionMessagePublisher.PublishAsync(
      message, outputService, callerName, callerFile, callerLine);
  }

  /// <summary>
  /// Выводит сообщение о настройке мультиметра, если включён вывод параметров выполнения.
  /// </summary>
  public static Task ShowMultimeterSetupAsync(
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (!DeviceDisplayConfig.GetExecutionParametersVisibility())
    {
      return Task.CompletedTask;
    }

    var message = ExecutionMessageBuilder.BuildMultimeterSetupMessage();
    return ExecutionMessagePublisher.PublishAsync(
      message, outputService, callerName, callerFile, callerLine);
  }

  /// <summary>
  /// Выводит сообщение о настройке пробойной установки, если включён вывод параметров выполнения.
  /// </summary>
  public static Task ShowBreakdownTesterSetupAsync(
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (!DeviceDisplayConfig.GetExecutionParametersVisibility())
    {
      return Task.CompletedTask;
    }

    var message = ExecutionMessageBuilder.BuildBreakdownTesterSetupMessage();
    return ExecutionMessagePublisher.PublishAsync(
      message, outputService, callerName, callerFile, callerLine);
  }

  /// <summary>
  /// Выводит заголовок начала инициализации оборудования.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishEquipmentInitializationAsync(
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return ExecutionMessagePublisher.PublishAsync(
      ExecutionMessageBuilder.BuildEquipmentInitializationMessage(),
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart: true);
  }

  /// <summary>
  /// Выводит заголовок начала настройки оборудования.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishEquipmentSetupAsync(
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return ExecutionMessagePublisher.PublishAsync(
      ExecutionMessageBuilder.BuildEquipmentSetupMessage(),
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart: true);
  }

  /// <summary>
  /// Выводит сообщение о завершении инициализации и начале теста.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishTestStartedAsync(
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return ExecutionMessagePublisher.PublishAsync(
      ExecutionMessageBuilder.BuildTestStartedMessage(),
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart: true);
  }
}
