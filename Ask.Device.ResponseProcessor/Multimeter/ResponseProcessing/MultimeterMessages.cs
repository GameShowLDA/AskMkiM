using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Protocol.Messages.EntryPoints;

namespace Ask.Device.ResponseProcessor.Multimeter.ResponseProcessing;

/// <summary>
/// Централизует сообщения протокола, связанные с работой и самоконтролем мультиметров.
/// </summary>
public static class MultimeterMessages
{
  /// <summary>
  /// Публикует сообщение о настройке мультиметра.
  /// </summary>
  /// <param name="outputService">Сервис вывода сообщения.</param>
  /// <returns>Задача публикации сообщения.</returns>
  public static Task PublishSetupAsync(IUserInteractionService? outputService)
    => ExecutionMessages.PublishMultimeterSetupAsync(outputService);

  /// <summary>
  /// Публикует результат операции мультиметра без дополнительного описания.
  /// </summary>
  /// <param name="device">Мультиметр, выполнивший операцию.</param>
  /// <param name="operation">Название операции.</param>
  /// <param name="result">Результат операции.</param>
  /// <param name="indentLevel">Уровень отступа.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <param name="isStepCheckpoint">Признак контрольной точки пошагового режима.</param>
  /// <returns>Задача публикации сообщения.</returns>
  public static Task PublishOperationResultAsync(
    IMultimeter device,
    string operation,
    bool result,
    int indentLevel,
    IUserInteractionService? outputService = null,
    bool isStepCheckpoint = false)
    => DeviceMessages.PublishOperationResultAsync(
      device, operation, result, indentLevel, outputService, isStepCheckpoint);

  /// <summary>
  /// Публикует результат операции мультиметра с дополнительным описанием.
  /// </summary>
  /// <param name="device">Мультиметр, выполнивший операцию.</param>
  /// <param name="operation">Название операции.</param>
  /// <param name="detail">Дополнительное описание результата.</param>
  /// <param name="result">Результат операции.</param>
  /// <param name="indentLevel">Уровень отступа.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <param name="isStepCheckpoint">Признак контрольной точки пошагового режима.</param>
  /// <returns>Задача публикации сообщения.</returns>
  public static Task PublishOperationResultAsync(
    IMultimeter device,
    string operation,
    string? detail,
    bool result,
    int indentLevel,
    IUserInteractionService? outputService = null,
    bool isStepCheckpoint = false)
    => DeviceMessages.PublishOperationResultAsync(
      device, operation, detail, result, indentLevel, outputService, isStepCheckpoint);

  /// <summary>
  /// Публикует команду этапа самоконтроля мультиметра.
  /// </summary>
  /// <param name="header">Заголовок команды.</param>
  /// <param name="outputService">Сервис вывода сообщения.</param>
  /// <param name="message">Дополнительный текст команды.</param>
  /// <param name="indentLevel">Уровень отступа.</param>
  /// <param name="onlyWhenStepMode">Признак вывода только в пошаговом режиме.</param>
  /// <returns>Задача публикации сообщения.</returns>
  public static Task PublishSelfTestCommandAsync(
    string header,
    IUserInteractionService outputService,
    string? message = null,
    int indentLevel = 0,
    bool onlyWhenStepMode = false)
    => SelfTestMessages.PublishCommandAsync(
      header, outputService, message, indentLevel, onlyWhenStepMode);

  /// <summary>
  /// Публикует результат измерения при самоконтроле мультиметра.
  /// </summary>
  /// <param name="status">Результат проверки измеренного значения.</param>
  /// <param name="result">Измеренное значение.</param>
  /// <param name="parameter">Название измеряемого параметра.</param>
  /// <param name="unit">Единица измерения.</param>
  /// <param name="idealResult">Ожидаемое значение.</param>
  /// <param name="percentageError">Допустимая погрешность в процентах.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Задача публикации сообщения.</returns>
  public static Task PublishSelfTestMeasurementResultAsync(
    bool status,
    double result,
    string parameter,
    string unit,
    double idealResult,
    int percentageError,
    IUserInteractionService outputService)
    => SelfTestMessages.PublishMultimeterMeasurementResultAsync(
      status, result, parameter, unit, idealResult, percentageError, outputService);

  /// <summary>
  /// Публикует результат проверки активного сопротивления конденсатора.
  /// </summary>
  /// <param name="result">Измеренное сопротивление.</param>
  /// <param name="isCorrect">Результат проверки сопротивления.</param>
  /// <param name="capacitanceValue">Проверяемое значение ёмкости.</param>
  /// <param name="minimumResistance">Минимально допустимое сопротивление.</param>
  /// <param name="unit">Единица измерения сопротивления.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Задача публикации сообщения.</returns>
  public static Task PublishActiveResistanceResultAsync(
    double result,
    bool isCorrect,
    string capacitanceValue,
    double minimumResistance,
    string unit,
    IUserInteractionService outputService)
    => SelfTestMessages.PublishActiveResistanceResultAsync(
      result, isCorrect, capacitanceValue, minimumResistance, unit, outputService);
}
