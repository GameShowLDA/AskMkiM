using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.ResponseProcessor.Multimeter.ResponseModels;
using Ask.Device.ResponseProcessor.Multimeter.ResponseProcessing.Checkers;
using Ask.Protocol.Messages.EntryPoints;

namespace Ask.Device.ResponseProcessor.Multimeter.ResponseProcessing;

/// <summary>
/// Предоставляет единую точку входа для обработки ответов мультиметров.
/// </summary>
public static class MultimeterResponseProcessor
{
  /// <summary>
  /// Проверяет непустой ответ идентификации мультиметра.
  /// </summary>
  /// <param name="response">Ответ на команду идентификации.</param>
  /// <returns><see langword="true"/>, если прибор вернул ответ.</returns>
  public static bool CheckInitialization(string response)
    => !string.IsNullOrWhiteSpace(response);

  /// <summary>
  /// Проверяет ответ на запрос текущего режима мультиметра.
  /// </summary>
  /// <param name="response">Ответ мультиметра.</param>
  /// <param name="expectedMode">Ожидаемый идентификатор режима.</param>
  /// <returns><see langword="true"/>, если ответ содержит ожидаемый режим.</returns>
  public static bool CheckMode(string response, string expectedMode)
    => ModeResponseChecker.Check(response, expectedMode);

  /// <summary>
  /// Преобразует измерительный ответ мультиметра.
  /// </summary>
  /// <param name="response">Ответ мультиметра.</param>
  /// <param name="result">Результат преобразования.</param>
  /// <returns><see langword="true"/>, если ответ содержит корректное число.</returns>
  public static bool TryParseMeasurement(string response, out MeasurementResponse? result)
    => MeasurementResponseChecker.TryParse(response, out result);

  /// <summary>
  /// Проверяет ответ режима прозвонки.
  /// </summary>
  /// <param name="response">Измерительный ответ мультиметра.</param>
  /// <param name="expectedClosed">Ожидаемое состояние замкнутой цепи.</param>
  /// <param name="matchesExpected">Результат сравнения состояния цепи.</param>
  /// <returns><see langword="true"/>, если ответ корректно обработан.</returns>
  public static bool TryCheckContinuity(
    string response,
    bool expectedClosed,
    out bool matchesExpected)
    => ContinuityResponseChecker.TryCheck(response, expectedClosed, out matchesExpected);

  /// <summary>
  /// Проверяет отсутствие ошибки прибора.
  /// </summary>
  /// <param name="response">Ответ на команду <c>SYSTEM:ERROR?</c>.</param>
  /// <param name="error">Разобранный ответ прибора.</param>
  /// <returns><see langword="true"/>, если прибор вернул нулевой код ошибки.</returns>
  public static bool CheckNoInstrumentError(
    string response,
    out InstrumentErrorResponse? error)
    => InstrumentErrorResponseChecker.TryParse(response, out error) && error!.Code == 0;

  /// <summary>
  /// Публикует результат подключения мультиметра.
  /// </summary>
  /// <param name="device">Подключаемый мультиметр.</param>
  /// <param name="result">Результат подключения.</param>
  /// <param name="error">Описание ошибки подключения.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Задача публикации сообщения.</returns>
  public static Task PublishConnectionResultAsync(
    IMultimeter device,
    bool result,
    string? error,
    IUserInteractionService? outputService = null)
    => EquipmentMessages.PublishConnectionResultAsync(device, result, error, outputService);

  /// <summary>
  /// Публикует результат отключения мультиметра.
  /// </summary>
  /// <param name="device">Отключаемый мультиметр.</param>
  /// <param name="result">Результат отключения.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Задача публикации сообщения.</returns>
  public static Task PublishDisconnectionResultAsync(
    IMultimeter device,
    bool result,
    IUserInteractionService? outputService = null)
    => EquipmentMessages.PublishDisconnectionResultAsync(device, result, outputService: outputService);

  /// <summary>
  /// Публикует результат инициализации мультиметра.
  /// </summary>
  /// <param name="device">Инициализируемый мультиметр.</param>
  /// <param name="result">Результат инициализации.</param>
  /// <param name="error">Ответ прибора или описание ошибки.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Задача публикации сообщения.</returns>
  public static Task PublishInitializationResultAsync(
    IMultimeter device,
    bool result,
    string? error,
    IUserInteractionService? outputService = null)
    => EquipmentMessages.PublishInitializationResultAsync(device, result, error, outputService);

  /// <summary>
  /// Публикует результат сброса мультиметра.
  /// </summary>
  /// <param name="device">Сбрасываемый мультиметр.</param>
  /// <param name="result">Результат сброса.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Задача публикации сообщения.</returns>
  public static Task PublishResetResultAsync(
    IMultimeter device,
    bool result,
    IUserInteractionService? outputService = null)
    => EquipmentMessages.PublishResetResultAsync(device, result, outputService: outputService);
}
