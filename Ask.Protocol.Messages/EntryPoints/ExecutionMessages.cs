using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
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

  /// <summary>
  /// Выводит заголовок этапа теста.
  /// </summary>
  /// <param name="title">Заголовок выполняемого этапа.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishTestStageAsync(
    string title,
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return ExecutionMessagePublisher.PublishAsync(
      ExecutionMessageBuilder.BuildTestStageMessage(title),
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart: true);
  }

  /// <summary>
  /// Выводит заголовок проверки точки.
  /// </summary>
  /// <param name="pointNumber">Номер проверяемой точки.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishTestPointAsync(
    int pointNumber,
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return ExecutionMessagePublisher.PublishAsync(
      ExecutionMessageBuilder.BuildTestPointMessage(pointNumber),
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Выводит результат операции с выбранным текстом для успешного и ошибочного исхода.
  /// </summary>
  /// <param name="isSuccessful">Признак успешного выполнения операции.</param>
  /// <param name="successHeader">Заголовок успешного результата.</param>
  /// <param name="successMessage">Описание успешного результата.</param>
  /// <param name="errorHeader">Заголовок ошибочного результата.</param>
  /// <param name="errorMessage">Описание ошибочного результата.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishOperationResultAsync(
    bool isSuccessful,
    string successHeader,
    string successMessage,
    string errorHeader,
    string errorMessage,
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return ExecutionMessagePublisher.PublishAsync(
      ExecutionMessageBuilder.BuildOperationResultMessage(
        isSuccessful,
        successHeader,
        successMessage,
        errorHeader,
        errorMessage),
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Выводит сообщение об ошибке выполнения.
  /// </summary>
  /// <param name="details">Описание ошибки.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishErrorAsync(
    string details,
    IMessageOutputService? outputService,
    bool skipStepModeCheck = false,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return ExecutionMessagePublisher.PublishAsync(
      ExecutionMessageBuilder.BuildErrorMessage(details),
      outputService,
      callerName,
      callerFile,
      callerLine,
      skipStepModeCheck: skipStepModeCheck);
  }

  /// <summary>
  /// Выводит заголовок инициализации устройств, если включён вывод параметров выполнения.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task ShowDevicesInitializationAsync(
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (!DeviceDisplayConfig.GetExecutionParametersVisibility())
    {
      return Task.CompletedTask;
    }

    return ExecutionMessagePublisher.PublishAsync(
      ExecutionMessageBuilder.BuildDevicesInitializationMessage(),
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart: true);
  }

  /// <summary>
  /// Выводит заголовок настройки измерителя, если включён вывод параметров выполнения.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task ShowMeasurementDeviceSetupAsync(
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (!DeviceDisplayConfig.GetExecutionParametersVisibility())
    {
      return Task.CompletedTask;
    }

    return ExecutionMessagePublisher.PublishAsync(
      ExecutionMessageBuilder.BuildMeasurementDeviceSetupMessage(),
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart: true);
  }

  /// <summary>
  /// Выводит заголовок подключения шин, если включён вывод сведений о коммутации.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task ShowBusConnectionAsync(
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (!DeviceDisplayConfig.GetConnectionInfoVisibility())
    {
      return Task.CompletedTask;
    }

    return ExecutionMessagePublisher.PublishAsync(
      ExecutionMessageBuilder.BuildBusConnectionMessage(),
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart: true);
  }

  /// <summary>
  /// Выводит заголовок подключения точек, если включён вывод сведений о коммутации.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task ShowPointConnectionAsync(
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (!DeviceDisplayConfig.GetConnectionInfoVisibility())
    {
      return Task.CompletedTask;
    }

    return ExecutionMessagePublisher.PublishAsync(
      ExecutionMessageBuilder.BuildPointConnectionMessage(),
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart: true);
  }

  /// <summary>
  /// Выводит заголовок подключения заданной точки, если включён вывод сведений о коммутации.
  /// </summary>
  /// <param name="point">Подключаемая точка.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task ShowPointConnectionAsync(
    PointModel point,
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (!DeviceDisplayConfig.GetConnectionInfoVisibility())
    {
      return Task.CompletedTask;
    }

    return ExecutionMessagePublisher.PublishAsync(
      ExecutionMessageBuilder.BuildPointConnectionMessage(point),
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart: true);
  }

  /// <summary>
  /// Выводит заголовок отключения заданной точки, если включён вывод сведений о коммутации.
  /// </summary>
  /// <param name="point">Отключаемая точка.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task ShowPointDisconnectionAsync(
    PointModel point,
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (!DeviceDisplayConfig.GetConnectionInfoVisibility())
    {
      return Task.CompletedTask;
    }

    return ExecutionMessagePublisher.PublishAsync(
      ExecutionMessageBuilder.BuildPointDisconnectionMessage(point),
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart: true);
  }

  /// <summary>
  /// Выводит заголовок отключения точек, если включён вывод сведений о коммутации.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task ShowPointsDisconnectionAsync(
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (!DeviceDisplayConfig.GetConnectionInfoVisibility())
    {
      return Task.CompletedTask;
    }

    return ExecutionMessagePublisher.PublishAsync(
      ExecutionMessageBuilder.BuildPointsDisconnectionMessage(),
      outputService,
      callerName,
      callerFile,
      callerLine,
      isBlockStart: true);
  }

  /// <summary>
  /// Выводит сообщение о сбросе всех точек коммутации.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishPointsResetAsync(
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return ExecutionMessagePublisher.PublishAsync(
      ExecutionMessageBuilder.BuildPointsResetMessage(),
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Выводит продолжительность задержки перед включением оборудования.
  /// </summary>
  /// <param name="seconds">Продолжительность задержки в секундах.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishDelayBeforeEnablingAsync(
    double? seconds,
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return ExecutionMessagePublisher.PublishAsync(
      ExecutionMessageBuilder.BuildDelayBeforeEnablingMessage(seconds),
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Выводит продолжительность задержки перед отключением оборудования.
  /// </summary>
  /// <param name="seconds">Продолжительность задержки в секундах.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishDelayBeforeDisablingAsync(
    double? seconds,
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return ExecutionMessagePublisher.PublishAsync(
      ExecutionMessageBuilder.BuildDelayBeforeDisablingMessage(seconds),
      outputService,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Выводит отладочное сообщение выполнения.
  /// </summary>
  /// <param name="details">Текст отладочного сообщения.</param>
  /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishDebugAsync(
    string details,
    IMessageOutputService? outputService,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return ExecutionMessagePublisher.PublishAsync(
      ExecutionMessageBuilder.BuildDebugMessage(details),
      outputService,
      callerName,
      callerFile,
      callerLine);
  }
}
