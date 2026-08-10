using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Protocol.Messages.Builders;
using Ask.Protocol.Messages.Show;
using System.Runtime.CompilerServices;

namespace Ask.Protocol.Messages.EntryPoints;

/// <summary>
/// Предоставляет единые точки публикации сообщений об операциях с устройствами.
/// </summary>
public static class DeviceMessages
{
  /// <summary>
  /// Публикует результат операции с устройством.
  /// </summary>
  /// <param name="device">Устройство, над которым выполнена операция.</param>
  /// <param name="operation">Название выполненной операции.</param>
  /// <param name="isSuccessful">Признак успешного выполнения операции.</param>
  /// <param name="indentLevel">Уровень отступа сообщения.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="isStepCheckpoint">Признак контрольной точки пошагового режима.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishOperationResultAsync(
    IAttachableDevice device,
    string operation,
    bool isSuccessful,
    int indentLevel,
    IUserInteractionService? outputService = null,
    bool isStepCheckpoint = false,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    return PublishOperationResultAsync(
      device,
      operation,
      details: null,
      isSuccessful,
      indentLevel,
      outputService,
      isStepCheckpoint,
      callerName,
      callerFile,
      callerLine);
  }

  /// <summary>
  /// Публикует результат операции с устройством с дополнительными сведениями.
  /// </summary>
  /// <param name="device">Устройство, над которым выполнена операция.</param>
  /// <param name="operation">Название выполненной операции.</param>
  /// <param name="details">Дополнительные сведения об операции.</param>
  /// <param name="isSuccessful">Признак успешного выполнения операции.</param>
  /// <param name="indentLevel">Уровень отступа сообщения.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="isStepCheckpoint">Признак контрольной точки пошагового режима.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая публикацию сообщения.</returns>
  public static Task PublishOperationResultAsync(
    IAttachableDevice device,
    string operation,
    string? details,
    bool isSuccessful,
    int indentLevel,
    IUserInteractionService? outputService = null,
    bool isStepCheckpoint = false,
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    if (outputService == null || !DeviceDisplayConfig.ShouldDisplayOperationResult(isSuccessful))
    {
      return Task.CompletedTask;
    }

    var message = EquipmentMessageBuilder.BuildDeviceOperationResult(
      device,
      operation,
      details,
      isSuccessful,
      indentLevel,
      isStepCheckpoint);

    return EquipmentMessagePublisher.PublishAsync(
      message,
      outputService,
      callerName,
      callerFile,
      callerLine,
      logToDeviceJournal: false,
      isBlockStart: isStepCheckpoint);
  }
}
