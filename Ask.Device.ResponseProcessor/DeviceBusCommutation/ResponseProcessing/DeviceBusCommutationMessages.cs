using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Protocol.Messages.EntryPoints;

namespace Ask.Device.ResponseProcessor.DeviceBusCommutation.ResponseProcessing;

/// <summary>
/// Централизует сообщения протокола, связанные с ответами и самоконтролем УКШ.
/// </summary>
public static class DeviceBusCommutationMessages
{
  /// <summary>
  /// Публикует заголовок самоконтроля УКШ.
  /// </summary>
  /// <param name="device">Проверяемое устройство.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <returns>Задача публикации сообщения.</returns>
  public static Task PublishSelfTestTitleAsync(ISwitchingDevice device, IMessageOutputService outputService)
    => EquipmentMessages.PublishDeviceHealthCheckTitleAsync(device, outputService);

  /// <summary>
  /// Публикует информационное сообщение самоконтроля УКШ.
  /// </summary>
  /// <param name="header">Заголовок сообщения.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="message">Дополнительный текст сообщения.</param>
  /// <param name="indentLevel">Уровень отступа.</param>
  /// <param name="isBlockStart">Признак начала логического блока.</param>
  /// <param name="skipPause">Признак публикации без ожидания паузы или пошагового режима.</param>
  /// <param name="ignoreOutputValidation">Признак публикации без проверки доступности сервиса вывода.</param>
  /// <returns>Задача публикации сообщения.</returns>
  public static Task PublishInformationAsync(
    string header, IMessageOutputService? outputService, string? message = null, int indentLevel = 0,
    bool isBlockStart = false, bool skipPause = false, bool ignoreOutputValidation = false)
    => SelfTestMessages.PublishInformationAsync(
      header, outputService, message, indentLevel, isBlockStart, skipPause, ignoreOutputValidation);

  /// <summary>
  /// Публикует сообщение об ошибке самоконтроля УКШ.
  /// </summary>
  /// <param name="details">Описание ошибки.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="header">Заголовок сообщения.</param>
  /// <param name="indentLevel">Уровень отступа.</param>
  /// <param name="skipPause">Признак публикации без ожидания паузы или пошагового режима.</param>
  /// <returns>Задача публикации сообщения.</returns>
  public static Task PublishErrorAsync(
    string details, IMessageOutputService? outputService, string header = "Ошибка",
    int indentLevel = 0, bool skipPause = false)
    => SelfTestMessages.PublishErrorAsync(details, outputService, header, indentLevel, skipPause);

  /// <summary>
  /// Публикует результат этапа самоконтроля УКШ.
  /// </summary>
  /// <param name="header">Заголовок проверяемого этапа.</param>
  /// <param name="isSuccessful">Результат выполнения этапа.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="message">Дополнительный текст результата.</param>
  /// <param name="indentLevel">Уровень отступа.</param>
  /// <param name="executionErrorMessage">Описание ошибки выполнения для накопления в протоколе.</param>
  /// <param name="executionError">Признак ошибки выполнения.</param>
  /// <param name="canBeDeleted">Признак возможности удалить сообщение при очистке успешных этапов.</param>
  /// <param name="isBlockStart">Признак начала логического блока.</param>
  /// <param name="skipPause">Признак публикации без ожидания паузы или пошагового режима.</param>
  /// <param name="isStepModeCheckpoint">Признак контрольной точки пошагового режима.</param>
  /// <returns>Задача публикации сообщения.</returns>
  public static Task PublishResultAsync(
    string header, bool isSuccessful, IMessageOutputService? outputService, string? message = null,
    int indentLevel = 0, string? executionErrorMessage = null, bool? executionError = null,
    bool? canBeDeleted = null, bool isBlockStart = false, bool skipPause = true,
    bool isStepModeCheckpoint = false)
    => SelfTestMessages.PublishResultAsync(
      header, isSuccessful, outputService, message, indentLevel, executionErrorMessage,
      executionError, canBeDeleted, isBlockStart, skipPause, isStepModeCheckpoint);
}
