using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;

using Ask.Protocol.Messages.Builders;
using Ask.Protocol.Messages.Show;

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
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  public static Task PublishConnectionResultAsync(
    IAttachableDevice device,
    bool isSuccessful,
    string? details = null,
    IMessageOutputService? outputService = null)
  {
    if (!isSuccessful || DeviceDisplayConfig.GetExecutionParametersVisibility())
    {
      var message = EquipmentMessageBuilder.BuildConnectionResult(device, isSuccessful, details);
      return EquipmentMessagePublisher.PublishAsync(message, outputService);
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
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  public static Task PublishDisconnectionResultAsync(
    IAttachableDevice device,
    bool isSuccessful,
    string? details = null,
    IMessageOutputService? outputService = null)
  {
    if (!isSuccessful || DeviceDisplayConfig.GetExecutionParametersVisibility())
    {
      var message = EquipmentMessageBuilder.BuildDisconnectionResult(device, isSuccessful, details);
      return EquipmentMessagePublisher.PublishAsync(message, outputService);
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
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  public static Task PublishInitializationResultAsync(
    IAttachableDevice device,
    bool isSuccessful,
    string? details = null,
    IMessageOutputService? outputService = null)
  {
    if (!isSuccessful || DeviceDisplayConfig.GetExecutionParametersVisibility())
    {
      var message = EquipmentMessageBuilder.BuildInitializationResult(device, isSuccessful, details);
      return EquipmentMessagePublisher.PublishAsync(message, outputService);
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
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  public static Task PublishConfigurationResultAsync(
    IAttachableDevice device,
    bool isSuccessful,
    string? details = null,
    IMessageOutputService? outputService = null)
  {
    if (!isSuccessful || DeviceDisplayConfig.GetExecutionParametersVisibility())
    {
      var message = EquipmentMessageBuilder.BuildConfigurationResult(device, isSuccessful, details);
      return EquipmentMessagePublisher.PublishAsync(message, outputService);
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
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  public static Task PublishResetResultAsync(
    IAttachableDevice device,
    bool isSuccessful,
    string? details = null,
    IMessageOutputService? outputService = null)
  {
    if (!isSuccessful || DeviceDisplayConfig.GetExecutionParametersVisibility())
    {
      var message = EquipmentMessageBuilder.BuildResetResult(device, isSuccessful, details);
      return EquipmentMessagePublisher.PublishAsync(message, outputService);
    }

    return Task.CompletedTask;
  }
}
