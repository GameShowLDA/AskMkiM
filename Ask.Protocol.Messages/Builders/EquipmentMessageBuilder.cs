using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System.Windows;
using System.Windows.Media;

namespace Ask.Protocol.Messages.Builders;

/// <summary>
/// Формирует модели сообщений о результатах операций с оборудованием.
/// </summary>
internal static class EquipmentMessageBuilder
{
  /// <summary>
  /// Формирует заголовок проверки работоспособности устройства.
  /// </summary>
  /// <param name="device">Проверяемое устройство.</param>
  /// <returns>Заголовок проверки работоспособности устройства.</returns>
  internal static ShowMessageModel BuildHealthCheckTitle(IAttachableDevice device)
  {
    ArgumentNullException.ThrowIfNull(device);

    return new ShowMessageModel(
      header: "Тест контроля работоспособности",
      message: $"{device.Name} {device.NumberChassis}.{device.Number}",
      type: ShowMessageModel.MessageType.CommandBlock);
  }

  /// <summary>
  /// Формирует сообщение о результате подключения устройства.
  /// </summary>
  /// <param name="device">Подключаемое устройство.</param>
  /// <param name="isSuccessful">Признак успешного подключения.</param>
  /// <param name="details">Описание ошибки подключения.</param>
  /// <returns>Модель сообщения о результате подключения устройства.</returns>
  internal static ShowMessageModel BuildConnectionResult(
    IAttachableDevice device,
    bool isSuccessful,
    string? details = null)
  {
    return BuildOperationResult(device, "Подключение", isSuccessful, details);
  }

  /// <summary>
  /// Формирует сообщение о результате отключения устройства.
  /// </summary>
  /// <param name="device">Отключаемое устройство.</param>
  /// <param name="isSuccessful">Признак успешного отключения.</param>
  /// <param name="details">Описание ошибки отключения.</param>
  /// <returns>Модель сообщения о результате отключения устройства.</returns>
  internal static ShowMessageModel BuildDisconnectionResult(
    IAttachableDevice device,
    bool isSuccessful,
    string? details = null)
  {
    return BuildOperationResult(device, "Отключение", isSuccessful, details);
  }

  /// <summary>
  /// Формирует сообщение о результате инициализации устройства.
  /// </summary>
  /// <param name="device">Инициализируемое устройство.</param>
  /// <param name="isSuccessful">Признак успешной инициализации.</param>
  /// <param name="details">Описание ошибки инициализации.</param>
  /// <returns>Модель сообщения о результате инициализации устройства.</returns>
  internal static ShowMessageModel BuildInitializationResult(
    IAttachableDevice device,
    bool isSuccessful,
    string? details = null)
  {
    return BuildOperationResult(device, "Инициализация", isSuccessful, details);
  }

  /// <summary>
  /// Формирует сообщение о результате настройки устройства.
  /// </summary>
  /// <param name="device">Настраиваемое устройство.</param>
  /// <param name="isSuccessful">Признак успешной настройки.</param>
  /// <param name="details">Описание ошибки настройки.</param>
  /// <returns>Модель сообщения о результате настройки устройства.</returns>
  internal static ShowMessageModel BuildConfigurationResult(
    IAttachableDevice device,
    bool isSuccessful,
    string? details = null)
  {
    return BuildOperationResult(device, "Настройка", isSuccessful, details);
  }

  /// <summary>
  /// Формирует сообщение о результате сброса устройства.
  /// </summary>
  /// <param name="device">Сбрасываемое устройство.</param>
  /// <param name="isSuccessful">Признак успешного сброса.</param>
  /// <param name="details">Описание ошибки сброса.</param>
  /// <returns>Модель сообщения о результате сброса устройства.</returns>
  internal static ShowMessageModel BuildResetResult(
    IAttachableDevice device,
    bool isSuccessful,
    string? details = null)
  {
    return BuildOperationResult(device, "Сброс устройства", isSuccessful, details);
  }

  /// <summary>
  /// Формирует сообщение о результате коммутации точки МКР.
  /// </summary>
  /// <param name="device">Модуль коммутации реле.</param>
  /// <param name="pointNumber">Номер коммутируемой точки.</param>
  /// <param name="bus">Шина коммутации.</param>
  /// <param name="connect">Признак подключения точки.</param>
  /// <param name="isSuccessful">Признак успешного выполнения операции.</param>
  /// <returns>Модель сообщения о результате коммутации точки.</returns>
  internal static ShowMessageModel BuildPointOperationResult(
    IAttachableDevice device,
    int pointNumber,
    BusPoint bus,
    bool connect,
    bool isSuccessful)
  {
    ArgumentNullException.ThrowIfNull(device);

    string operation = connect
      ? $"Подключение точки {pointNumber} к шине [{bus}]"
      : $"Отключение точки {pointNumber} от шины [{bus}]";

    Color? fallbackMessageColor = Application.Current == null
      ? isSuccessful ? Colors.Green : Colors.Red
      : null;

    return new ShowMessageModel(
      header: $"{device.Name}({device.NumberChassis}.{device.Number}) - {operation}",
      messageColor: fallbackMessageColor,
      type: isSuccessful
        ? ShowMessageModel.MessageType.Success
        : ShowMessageModel.MessageType.Error)
    {
      IndentLevel = 1,
      IsDeviceMessage = true,
    };
  }

  /// <summary>
  /// Формирует сообщение о результате операции с устройством.
  /// </summary>
  /// <param name="device">Устройство, над которым выполнена операция.</param>
  /// <param name="operation">Название выполненной операции.</param>
  /// <param name="details">Дополнительные сведения об операции.</param>
  /// <param name="isSuccessful">Признак успешного выполнения операции.</param>
  /// <param name="indentLevel">Уровень отступа сообщения.</param>
  /// <param name="isStepModeCheckpoint">Признак контрольной точки пошагового режима.</param>
  /// <returns>Модель сообщения о результате операции.</returns>
  internal static ShowMessageModel BuildDeviceOperationResult(
    IAttachableDevice device,
    string operation,
    string? details,
    bool isSuccessful,
    int indentLevel,
    bool isStepModeCheckpoint)
  {
    ArgumentNullException.ThrowIfNull(device);

    Color? fallbackMessageColor = Application.Current == null
      ? isSuccessful ? Colors.Green : Colors.Red
      : null;

    return new ShowMessageModel(
      header: $"{device.Name}({device.NumberChassis}.{device.Number}) - {operation}",
      message: details,
      messageColor: fallbackMessageColor,
      type: isSuccessful
        ? ShowMessageModel.MessageType.Success
        : ShowMessageModel.MessageType.Error)
    {
      IndentLevel = indentLevel,
      IsDeviceMessage = true,
      IsStepModeCheckpoint = isStepModeCheckpoint,
    };
  }

  /// <summary>
  /// Формирует сообщение о результате операции с устройством.
  /// </summary>
  /// <param name="device">Устройство, над которым выполнена операция.</param>
  /// <param name="operation">Название выполненной операции.</param>
  /// <param name="isSuccessful">Признак успешного выполнения операции.</param>
  /// <param name="details">Описание ошибки выполнения операции.</param>
  /// <returns>Модель сообщения о результате операции с устройством.</returns>
  /// <exception cref="ArgumentNullException">
  /// Выбрасывается, если <paramref name="device"/> равен <see langword="null"/>.
  /// </exception>
  private static ShowMessageModel BuildOperationResult(
    IAttachableDevice device,
    string operation,
    bool isSuccessful,
    string? details)
  {
    ArgumentNullException.ThrowIfNull(device);

    return new ShowMessageModel(
      header: $"{device.Name}({device.NumberChassis}.{device.Number})",
      message: !isSuccessful && !string.IsNullOrWhiteSpace(details)
        ? $"{operation}: {details}"
        : operation,
      type: isSuccessful
        ? ShowMessageModel.MessageType.Success
        : ShowMessageModel.MessageType.Error)
    {
      IsDeviceMessage = true,
    };
  }
}
