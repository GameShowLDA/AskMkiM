using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.ResponseProcessor.DeviceBusCommutation.ResponseProcessing.Checkers;
using Ask.Protocol.Messages.EntryPoints;

namespace Ask.Device.ResponseProcessor.DeviceBusCommutation.ResponseProcessing;

/// <summary>
/// Предоставляет единую точку входа для проверки ответов УКШ и публикации результатов операций.
/// </summary>
public static class DeviceBusCommutationResponseProcessor
{
  /// <summary>
  /// Проверяет ответ инициализации УКШ.
  /// </summary>
  /// <param name="response">JSON-ответ УКШ.</param>
  /// <param name="device">Инициализируемое устройство.</param>
  /// <returns><see langword="true"/>, если ответ принадлежит указанному УКШ.</returns>
  public static bool CheckInitialization(string response, ISwitchingDevice device)
    => JsonCommandResponseChecker.Check(response, device);

  /// <summary>
  /// Проверяет ответ сброса УКШ.
  /// </summary>
  /// <param name="response">JSON-ответ УКШ.</param>
  /// <param name="device">Сбрасываемое устройство.</param>
  /// <returns><see langword="true"/>, если УКШ подтвердил команду сброса.</returns>
  public static bool CheckReset(string response, ISwitchingDevice device)
    => JsonCommandResponseChecker.Check(response, device, "2.0.1.");

  /// <summary>
  /// Проверяет произвольное JSON-подтверждение команды УКШ.
  /// </summary>
  /// <param name="response">JSON-ответ УКШ.</param>
  /// <param name="device">УКШ, которому была отправлена команда.</param>
  /// <param name="expectedAnswer">Ожидаемое значение поля <c>Answer</c>.</param>
  /// <returns><see langword="true"/>, если адрес и подтверждение совпадают с ожидаемыми.</returns>
  public static bool CheckJsonCommand(string response, ISwitchingDevice device, string expectedAnswer)
    => JsonCommandResponseChecker.Check(response, device, expectedAnswer);

  /// <summary>
  /// Проверяет произвольный числовой ответ УКШ.
  /// </summary>
  /// <param name="response">Числовой ответ УКШ.</param>
  /// <param name="expectedValue">Ожидаемое значение.</param>
  /// <returns><see langword="true"/>, если ответ содержит ожидаемое значение.</returns>
  public static bool CheckNumericCommand(string response, int expectedValue)
    => NumericCommandResponseChecker.Check(response, expectedValue);

  /// <summary>
  /// Преобразует числовой ответ УКШ.
  /// </summary>
  /// <param name="response">Числовой ответ УКШ.</param>
  /// <param name="value">Полученное значение.</param>
  /// <returns><see langword="true"/>, если ответ успешно преобразован.</returns>
  public static bool TryReadNumericResponse(string response, out int value)
    => NumericCommandResponseChecker.TryRead(response, out value);

  /// <summary>
  /// Проверяет подтверждение управления реле цепи самоконтроля.
  /// </summary>
  /// <param name="response">Числовой ответ УКШ.</param>
  /// <returns><see langword="true"/>, если команда подтверждена.</returns>
  public static bool CheckSelfTestRelayControl(string response)
    => SelfTestCommandResponseChecker.CheckRelayControl(response);

  /// <summary>
  /// Проверяет подтверждение коммутации цепи самоконтроля.
  /// </summary>
  /// <param name="response">JSON-ответ УКШ.</param>
  /// <param name="device">УКШ, которому была отправлена команда.</param>
  /// <param name="connectorType">Тип проверяемой цепи.</param>
  /// <param name="busContact">Контакт шины.</param>
  /// <param name="action">Код действия над цепью.</param>
  /// <returns><see langword="true"/>, если ответ соответствует команде.</returns>
  public static bool CheckSelfTestCircuitControl(
    string response,
    ISwitchingDevice device,
    int connectorType,
    int busContact,
    int action)
    => SelfTestCommandResponseChecker.CheckCircuitControl(
      response, device, connectorType, busContact, action);

  /// <summary>
  /// Проверяет и публикует результат управления отдельным реле.
  /// </summary>
  /// <param name="response">Числовой ответ УКШ.</param>
  /// <param name="device">УКШ, которому была отправлена команда.</param>
  /// <param name="relayNumber">Номер управляемого реле.</param>
  /// <param name="connect">Признак подключения реле.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Результат проверки ответа.</returns>
  public static Task<bool> CheckRelayOperationAsync(
    string response,
    ISwitchingDevice device,
    int relayNumber,
    bool connect,
    IUserInteractionService? outputService = null)
    => CheckConnectionResultAsync(
      RelayCommandResponseChecker.Check(response, relayNumber),
      device,
      $"{(connect ? "Подключение" : "Отключение")} реле",
      $"№{relayNumber}",
      outputService);

  /// <summary>
  /// Проверяет и публикует результат коммутации цепи.
  /// </summary>
  /// <param name="response">Числовой ответ УКШ.</param>
  /// <param name="device">УКШ, которому была отправлена команда.</param>
  /// <param name="connect">Признак подключения элемента.</param>
  /// <param name="elementName">Название коммутируемого элемента.</param>
  /// <param name="detail">Дополнительное описание элемента.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Результат проверки ответа.</returns>
  public static Task<bool> CheckChainOperationAsync(
    string response,
    ISwitchingDevice device,
    bool connect,
    string elementName,
    string detail,
    IUserInteractionService? outputService = null)
    => CheckConnectionResultAsync(
      ChainCommandResponseChecker.Check(response),
      device,
      $"{(connect ? "Подключение" : "Отключение")} {elementName}",
      detail,
      outputService);

  /// <summary>
  /// Проверяет и публикует результат коммутации оборудования с шиной УКШ.
  /// </summary>
  /// <param name="response">JSON-ответ УКШ.</param>
  /// <param name="device">УКШ, которому была отправлена команда.</param>
  /// <param name="connectorType">Код подключаемого оборудования.</param>
  /// <param name="busNumber">Номер шины.</param>
  /// <param name="connect">Признак подключения оборудования.</param>
  /// <param name="equipmentName">Название оборудования для протокола.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Результат проверки ответа.</returns>
  public static Task<bool> CheckEquipmentOperationAsync(
    string response,
    ISwitchingDevice device,
    int connectorType,
    int busNumber,
    bool connect,
    string equipmentName,
    IUserInteractionService? outputService = null)
    => CheckConnectionResultAsync(
      EquipmentCommandResponseChecker.Check(
        response, device, 5, connectorType, busNumber, connect ? 1 : 2),
      device,
      $"{(connect ? "Подключение" : "Отключение")} {equipmentName}",
      null,
      outputService);

  /// <summary>
  /// Проверяет и публикует результат подключения или отключения всех шин УКШ.
  /// </summary>
  /// <param name="response">JSON-ответ УКШ.</param>
  /// <param name="device">УКШ, которому была отправлена команда.</param>
  /// <param name="connect">Признак подключения шин.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Результат проверки ответа.</returns>
  public static Task<bool> CheckAllBusesOperationAsync(
    string response,
    ISwitchingDevice device,
    bool connect,
    IUserInteractionService? outputService = null)
    => CheckConnectionResultAsync(
      BusCommandResponseChecker.Check(response, device, connect),
      device,
      $"{(connect ? "Подключение" : "Отключение")} (AB1, AB2, AB3, AB4)",
      null,
      outputService);

  /// <summary>
  /// Проверяет и публикует результат управления вспомогательным реле УКШ.
  /// </summary>
  /// <param name="response">JSON-ответ УКШ.</param>
  /// <param name="device">УКШ, которому была отправлена команда.</param>
  /// <param name="accessoryType">Код группы вспомогательных реле.</param>
  /// <param name="elementNumber">Номер элемента в группе.</param>
  /// <param name="connect">Признак подключения элемента.</param>
  /// <param name="operation">Название операции для протокола.</param>
  /// <param name="detail">Дополнительное описание операции.</param>
  /// <param name="executionParameter">Признак применения настройки вывода параметров выполнения.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Результат проверки ответа.</returns>
  public static Task<bool> CheckAccessoryOperationAsync(
    string response,
    ISwitchingDevice device,
    int accessoryType,
    int elementNumber,
    bool connect,
    string operation,
    string? detail = null,
    bool executionParameter = false,
    IUserInteractionService? outputService = null)
  {
    bool result = EquipmentCommandResponseChecker.Check(
      response, device, 9, accessoryType, elementNumber, connect ? 1 : 2);
    return executionParameter
      ? CheckExecutionResultAsync(result, device, operation, detail, outputService)
      : CheckConnectionResultAsync(result, device, operation, detail, outputService);
  }

  /// <summary>
  /// Проверяет JSON-ответ и публикует результат операции подключения.
  /// </summary>
  /// <param name="response">JSON-ответ УКШ.</param>
  /// <param name="device">УКШ, которому была отправлена команда.</param>
  /// <param name="expectedAnswer">Ожидаемое подтверждение команды.</param>
  /// <param name="operation">Название операции для протокола.</param>
  /// <param name="detail">Дополнительное описание операции.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Результат проверки ответа.</returns>
  public static async Task<bool> CheckConnectionOperationAsync(
    string response,
    ISwitchingDevice device,
    string expectedAnswer,
    string operation,
    string? detail = null,
    IUserInteractionService? outputService = null)
  {
    bool result = CheckJsonCommand(response, device, expectedAnswer);
    if (!result || DeviceDisplayConfig.GetConnectionInfoVisibility())
    {
      await PublishOperationResultAsync(device, operation, detail, result, outputService);
    }

    return result;
  }

  /// <summary>
  /// Проверяет JSON-ответ и публикует результат изменения параметра выполнения.
  /// </summary>
  /// <param name="response">JSON-ответ УКШ.</param>
  /// <param name="device">УКШ, которому была отправлена команда.</param>
  /// <param name="expectedAnswer">Ожидаемое подтверждение команды.</param>
  /// <param name="operation">Название операции для протокола.</param>
  /// <param name="detail">Дополнительное описание операции.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Результат проверки ответа.</returns>
  public static async Task<bool> CheckExecutionOperationAsync(
    string response,
    ISwitchingDevice device,
    string expectedAnswer,
    string operation,
    string? detail = null,
    IUserInteractionService? outputService = null)
  {
    bool result = CheckJsonCommand(response, device, expectedAnswer);
    if (!result || DeviceDisplayConfig.GetExecutionParametersVisibility())
    {
      await PublishOperationResultAsync(device, operation, detail, result, outputService);
    }

    return result;
  }

  /// <summary>
  /// Проверяет числовой ответ и публикует результат операции подключения.
  /// </summary>
  /// <param name="response">Числовой ответ УКШ.</param>
  /// <param name="device">УКШ, которому была отправлена команда.</param>
  /// <param name="expectedValue">Ожидаемое значение ответа.</param>
  /// <param name="operation">Название операции для протокола.</param>
  /// <param name="detail">Дополнительное описание операции.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Результат проверки ответа.</returns>
  public static async Task<bool> CheckNumericConnectionOperationAsync(
    string response,
    ISwitchingDevice device,
    int expectedValue,
    string operation,
    string? detail = null,
    IUserInteractionService? outputService = null)
  {
    bool result = CheckNumericCommand(response, expectedValue);
    if (!result || DeviceDisplayConfig.GetConnectionInfoVisibility())
    {
      await PublishOperationResultAsync(device, operation, detail, result, outputService);
    }

    return result;
  }

  /// <summary>
  /// Публикует результат подключения УКШ.
  /// </summary>
  /// <param name="device">Подключаемое устройство.</param>
  /// <param name="result">Результат подключения.</param>
  /// <param name="error">Описание ошибки подключения.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Задача публикации сообщения.</returns>
  public static Task PublishConnectionResultAsync(
    ISwitchingDevice device,
    bool result,
    string? error,
    IUserInteractionService? outputService = null)
    => EquipmentMessages.PublishConnectionResultAsync(device, result, error, outputService);

  /// <summary>
  /// Публикует результат отключения УКШ.
  /// </summary>
  /// <param name="device">Отключаемое устройство.</param>
  /// <param name="result">Результат отключения.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Задача публикации сообщения.</returns>
  public static Task PublishDisconnectionResultAsync(
    ISwitchingDevice device,
    bool result,
    IUserInteractionService? outputService = null)
    => EquipmentMessages.PublishDisconnectionResultAsync(device, result, outputService: outputService);

  /// <summary>
  /// Публикует результат инициализации УКШ.
  /// </summary>
  /// <param name="device">Инициализируемое устройство.</param>
  /// <param name="result">Результат инициализации.</param>
  /// <param name="error">Описание ошибки инициализации.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Задача публикации сообщения.</returns>
  public static Task PublishInitializationResultAsync(
    ISwitchingDevice device,
    bool result,
    string? error,
    IUserInteractionService? outputService = null)
    => EquipmentMessages.PublishInitializationResultAsync(device, result, error, outputService);

  /// <summary>
  /// Публикует результат сброса УКШ.
  /// </summary>
  /// <param name="device">Сбрасываемое устройство.</param>
  /// <param name="result">Результат сброса.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Задача публикации сообщения.</returns>
  public static Task PublishResetResultAsync(
    ISwitchingDevice device,
    bool result,
    IUserInteractionService? outputService = null)
    => EquipmentMessages.PublishResetResultAsync(device, result, outputService: outputService);

  /// <summary>
  /// Публикует результат отдельной операции УКШ.
  /// </summary>
  /// <param name="device">Устройство, выполнившее операцию.</param>
  /// <param name="operation">Название операции.</param>
  /// <param name="detail">Дополнительное описание операции.</param>
  /// <param name="result">Результат выполнения операции.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Задача публикации сообщения.</returns>
  private static Task PublishOperationResultAsync(
    ISwitchingDevice device,
    string operation,
    string? detail,
    bool result,
    IUserInteractionService? outputService)
    => detail == null
      ? DeviceMessages.PublishOperationResultAsync(device, operation, result, 1, outputService)
      : DeviceMessages.PublishOperationResultAsync(device, operation, detail, result, 1, outputService);

  /// <summary>
  /// Публикует результат с учётом настройки отображения информации о подключениях.
  /// </summary>
  /// <param name="result">Результат проверки ответа.</param>
  /// <param name="device">Устройство, выполнившее операцию.</param>
  /// <param name="operation">Название операции.</param>
  /// <param name="detail">Дополнительное описание операции.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Результат проверки ответа.</returns>
  private static async Task<bool> CheckConnectionResultAsync(
    bool result,
    ISwitchingDevice device,
    string operation,
    string? detail,
    IUserInteractionService? outputService)
  {
    if (!result || DeviceDisplayConfig.GetConnectionInfoVisibility())
    {
      await PublishOperationResultAsync(device, operation, detail, result, outputService);
    }

    return result;
  }

  /// <summary>
  /// Публикует результат с учётом настройки отображения параметров выполнения.
  /// </summary>
  /// <param name="result">Результат проверки ответа.</param>
  /// <param name="device">Устройство, выполнившее операцию.</param>
  /// <param name="operation">Название операции.</param>
  /// <param name="detail">Дополнительное описание операции.</param>
  /// <param name="outputService">Сервис вывода результата.</param>
  /// <returns>Результат проверки ответа.</returns>
  private static async Task<bool> CheckExecutionResultAsync(
    bool result,
    ISwitchingDevice device,
    string operation,
    string? detail,
    IUserInteractionService? outputService)
  {
    if (!result || DeviceDisplayConfig.GetExecutionParametersVisibility())
    {
      await PublishOperationResultAsync(device, operation, detail, result, outputService);
    }

    return result;
  }
}
