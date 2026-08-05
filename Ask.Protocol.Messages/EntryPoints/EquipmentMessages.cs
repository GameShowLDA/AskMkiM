using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Protocol.Messages.Builders;
using Ask.Protocol.Messages.Show;
using System.Runtime.CompilerServices;

namespace Ask.Protocol.Messages.EntryPoints;

/// <summary>
/// Предоставляет единые точки формирования, логирования и вывода сообщений оборудования.
/// </summary>
public static class EquipmentMessages
{
  /// <summary>
  /// Публикует результат подключения устройства.
  /// </summary>
  /// <param name="device">Подключаемое устройство.</param>
  /// <param name="isSuccessful">Признак успешного подключения.</param>
  /// <param name="details">Описание ошибки подключения.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  public static Task PublishConnectionResultAsync(
    IDevice device,
    bool isSuccessful,
    string? details = null,
    IMessageOutputService? outputService = null,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (device is IAttachableDevice attachableDevice && ShouldPublish(isSuccessful))
    {
      var message = EquipmentMessageBuilder.BuildConnectionResult(attachableDevice, isSuccessful, details);
      return EquipmentMessagePublisher.PublishAsync(message, outputService, callerName, callerFile, callerLine);
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// Публикует результат отключения устройства.
  /// </summary>
  /// <param name="device">Отключаемое устройство.</param>
  /// <param name="isSuccessful">Признак успешного отключения.</param>
  /// <param name="details">Описание ошибки отключения.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  public static Task PublishDisconnectionResultAsync(
    IDevice device,
    bool isSuccessful,
    string? details = null,
    IMessageOutputService? outputService = null,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (device is IAttachableDevice attachableDevice && ShouldPublish(isSuccessful))
    {
      var message = EquipmentMessageBuilder.BuildDisconnectionResult(attachableDevice, isSuccessful, details);
      return EquipmentMessagePublisher.PublishAsync(message, outputService, callerName, callerFile, callerLine);
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// Публикует результат инициализации устройства.
  /// </summary>
  /// <param name="device">Инициализируемое устройство.</param>
  /// <param name="isSuccessful">Признак успешной инициализации.</param>
  /// <param name="details">Описание ошибки инициализации.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  public static Task PublishInitializationResultAsync(
    IDevice device,
    bool isSuccessful,
    string? details = null,
    IMessageOutputService? outputService = null,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (device is IAttachableDevice attachableDevice && ShouldPublish(isSuccessful))
    {
      var message = EquipmentMessageBuilder.BuildInitializationResult(attachableDevice, isSuccessful, details);
      return EquipmentMessagePublisher.PublishAsync(message, outputService, callerName, callerFile, callerLine);
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// Публикует результат настройки устройства.
  /// </summary>
  /// <param name="device">Настраиваемое устройство.</param>
  /// <param name="isSuccessful">Признак успешной настройки.</param>
  /// <param name="details">Описание ошибки настройки.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  public static Task PublishConfigurationResultAsync(
    IDevice device,
    bool isSuccessful,
    string? details = null,
    IMessageOutputService? outputService = null,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (device is IAttachableDevice attachableDevice && ShouldPublish(isSuccessful))
    {
      var message = EquipmentMessageBuilder.BuildConfigurationResult(attachableDevice, isSuccessful, details);
      return EquipmentMessagePublisher.PublishAsync(message, outputService, callerName, callerFile, callerLine);
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// Публикует результат сброса устройства.
  /// </summary>
  /// <param name="device">Сбрасываемое устройство.</param>
  /// <param name="isSuccessful">Признак успешного сброса.</param>
  /// <param name="details">Описание ошибки сброса.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  public static Task PublishResetResultAsync(
    IDevice device,
    bool isSuccessful,
    string? details = null,
    IMessageOutputService? outputService = null,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (device is IAttachableDevice attachableDevice && ShouldPublish(isSuccessful))
    {
      var message = EquipmentMessageBuilder.BuildResetResult(attachableDevice, isSuccessful, details);
      return EquipmentMessagePublisher.PublishAsync(message, outputService, callerName, callerFile, callerLine);
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// Проверяет, требуется ли публиковать результат операции.
  /// </summary>
  /// <param name="isSuccessful">Признак успешного выполнения операции.</param>
  /// <returns>
  /// <see langword="true"/>, если результат требуется опубликовать;
  /// в противном случае — <see langword="false"/>.
  /// </returns>
  private static bool ShouldPublish(bool isSuccessful)
  {
    return !isSuccessful || DeviceDisplayConfig.GetExecutionParametersVisibility();
  }
}
