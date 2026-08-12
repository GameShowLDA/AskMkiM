using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseModels;
using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing.Checkers;
using Ask.Protocol.Messages.EntryPoints;
using System.Text.Json;

namespace Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing;

/// <summary>
/// Предоставляет единую точку входа для обработки ответов МКР.
/// </summary>
public static class ModuleRelayControlResponseProcessor
{
  public static void EnsureCommandAccepted(string response, IRelaySwitchModule module, string command)
  {
    ArgumentNullException.ThrowIfNull(module);
    string? error = CommandStatusChecker.GetError(response);
    if (error == null)
    {
      return;
    }

    throw new ModuleRelayControlProtocolException(
      $"{module.Name} {module.NumberChassis}.{module.Number}",
      GetOperationName(command),
      error,
      CommandStatusChecker.GetStatus(response)!);
  }

  public static bool CheckInitialization(string response, IRelaySwitchModule module)
  {
    ArgumentNullException.ThrowIfNull(module);
    InitializationResponse? model = ResponseDeserializer.Deserialize<InitializationResponse>(response);
    return model != null &&
      ModuleResponseIdentityChecker.Check(model, module.NumberChassis, module.Number);
  }

  public static bool CheckReset(string response, IRelaySwitchModule module)
    => CheckCommandResponse(response, module, "2.0.1");

  /// <summary>
  /// Проверяет адрес отправителя и точное подтверждение произвольной обычной команды МКР.
  /// </summary>
  /// <param name="response">JSON-ответ МКР.</param>
  /// <param name="module">Модуль, которому была отправлена команда.</param>
  /// <param name="expectedAnswer">Ожидаемое значение поля <c>Answer</c>.</param>
  /// <returns>
  /// <see langword="true"/>, если адрес и подтверждение команды совпадают с ожидаемыми.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  public static bool CheckCommandResponse(
    string response,
    IRelaySwitchModule module,
    string expectedAnswer)
  {
    ArgumentNullException.ThrowIfNull(module);
    return CommandResponseChecker.Check(
      response,
      module.NumberChassis,
      module.Number,
      expectedAnswer);
  }

  public static Task PublishConnectionResultAsync(
    IRelaySwitchModule module,
    bool result,
    string? error,
    IUserInteractionService? outputService = null)
    => EquipmentMessages.PublishConnectionResultAsync(module, result, error, outputService);

  public static Task PublishDisconnectionResultAsync(
    IRelaySwitchModule module,
    bool result,
    IUserInteractionService? outputService = null)
    => EquipmentMessages.PublishDisconnectionResultAsync(module, result, outputService: outputService);

  public static Task PublishInitializationResultAsync(
    IRelaySwitchModule module,
    bool result,
    string? error,
    IUserInteractionService? outputService = null)
    => EquipmentMessages.PublishInitializationResultAsync(module, result, error, outputService);

  public static Task PublishResetResultAsync(
    IRelaySwitchModule module,
    bool result,
    IUserInteractionService? outputService = null)
    => EquipmentMessages.PublishResetResultAsync(module, result, outputService: outputService);

  public static Task PublishOperationResultAsync(
    IRelaySwitchModule module,
    string operation,
    bool result,
    IUserInteractionService? outputService = null)
    => DeviceMessages.PublishOperationResultAsync(module, operation, result, 1, outputService);

  public static Task PublishSelfTestTitleAsync(
    IRelaySwitchModule module,
    IUserInteractionService? outputService = null)
    => outputService is null
      ? Task.CompletedTask
      : EquipmentMessages.PublishDeviceHealthCheckTitleAsync(module, outputService);

  public static Task PublishSelfTestInformationAsync(
    string text,
    IUserInteractionService? outputService = null)
    => SelfTestMessages.PublishInformationAsync(text, outputService);

  public static Task PublishSelfTestResultAsync(
    string text,
    bool result,
    IUserInteractionService? outputService = null,
    bool skipPause = true)
    => SelfTestMessages.PublishResultAsync(text, result, outputService, skipPause: skipPause);

  public static async Task<bool> CheckBusOperationAsync(
    string response,
    IRelaySwitchModule module,
    SwitchingBus bus,
    int busType,
    int busNumber,
    bool connect,
    IUserInteractionService? outputService = null)
  {
    bool result = CheckCommandResponse(
      response,
      module,
      $"4.{busType}.{busNumber}.{(connect ? 1 : 2)}");
    await DeviceMessages.PublishOperationResultAsync(
      module,
      $"{(connect ? "Подключение" : "Отключение")} шины [{bus}]",
      result,
      1,
      outputService);
    return result;
  }

  public static async Task<bool> CheckMeterOperationAsync(
    string response,
    IRelaySwitchModule module,
    bool connect,
    IUserInteractionService? outputService = null)
  {
    bool result = CheckCommandResponse(response, module, $"5.{(connect ? 1 : 2)}");
    await DeviceMessages.PublishOperationResultAsync(
      module,
      $"{(connect ? "Подключение" : "Отключение")} измерителя модуля МКР",
      result,
      1,
      outputService);
    return result;
  }

  public static async Task<bool> CheckMeterStateAsync(
    string response,
    IRelaySwitchModule module,
    IUserInteractionService? outputService = null)
  {
    bool result = CheckCommandResponse(response, module, "7.1");
    await DeviceMessages.PublishOperationResultAsync(
      module,
      result ? "Обнаружено подключение шин/точек (МКР)" : "Подключение шин/точек не обнаружено (МКР)",
      result,
      1,
      outputService);
    return result;
  }

  public static async Task<bool> CheckPointRangeOperationAsync(
    string response,
    IRelaySwitchModule module,
    int firstPoint,
    int lastPoint,
    BusPoint bus,
    bool connect,
    IUserInteractionService? outputService = null)
  {
    int action = ((int)bus * 10) + (connect ? 1 : 2);
    bool result = CheckCommandResponse(response, module, $"11.{firstPoint}.{lastPoint}.{action}");
    string description = $"{firstPoint}-{lastPoint} {(connect ? "к" : "от")} шине [{bus}]";
    await DeviceMessages.PublishOperationResultAsync(
      module,
      $"{(connect ? "Подключение" : "Отключение")} диапазона точек {description}",
      result,
      1,
      outputService);
    return result;
  }

  public static async Task<bool> CheckPointReconnectionAsync(
    string response,
    IRelaySwitchModule module,
    int pointNumber,
    BusPoint bus,
    IUserInteractionService? outputService = null)
  {
    bool result = CheckCommandResponse(response, module, $"81.{pointNumber}.{(int)bus}.0");
    await DeviceMessages.PublishOperationResultAsync(
      module,
      $"Переподключение точки {pointNumber} к шине [{bus}]",
      result,
      1,
      outputService);
    return result;
  }

  /// <summary>
  /// Проверяет ответ самоконтроля внешней шины МКР без публикации результата.
  /// </summary>
  /// <param name="response">JSON-ответ МКР.</param>
  /// <param name="module">Модуль, выполнивший самоконтроль.</param>
  /// <param name="busNumber">Ожидаемый номер проверенной внешней шины.</param>
  /// <returns>
  /// <see langword="true"/>, если ответ подтверждает исправность внешней шины.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  public static bool CheckExternalBusSelfTest(
    string response,
    IRelaySwitchModule module,
    int busNumber)
  {
    ArgumentNullException.ThrowIfNull(module);
    return ExternalBusSelfTestChecker.Check(
      response,
      module.NumberChassis,
      module.Number,
      busNumber);
  }

  /// <summary>
  /// Проверяет ответ на подключение точки МКР.
  /// </summary>
  /// <param name="response">JSON-ответ МКР.</param>
  /// <param name="module">Модуль, которому отправлена команда.</param>
  /// <param name="pointNumber">Номер подключаемой точки.</param>
  /// <param name="busNumber">Номер шины подключения.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <returns><see langword="true"/>, если ответ соответствует отправленной команде.</returns>
  public static Task<bool> CheckPointConnectionAsync(
    string response,
    IRelaySwitchModule module,
    int pointNumber,
    int busNumber,
    IMessageOutputService? outputService = null)
  {
    return CheckPointOperationAsync(response, module, pointNumber, busNumber, connect: true,
      useHardwareVerification: false, outputService);
  }

  /// <summary>
  /// Проверяет ответ на подключение точки МКР с аппаратным контролем.
  /// </summary>
  /// <param name="response">JSON-ответ МКР.</param>
  /// <param name="module">Модуль, которому отправлена команда.</param>
  /// <param name="pointNumber">Номер подключаемой точки.</param>
  /// <param name="busNumber">Номер шины подключения.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <returns><see langword="true"/>, если ответ соответствует команде и состояние реле подтверждено.</returns>
  public static Task<bool> CheckVerifiedPointConnectionAsync(
    string response,
    IRelaySwitchModule module,
    int pointNumber,
    int busNumber,
    IMessageOutputService? outputService = null)
  {
    return CheckPointOperationAsync(response, module, pointNumber, busNumber, connect: true,
      useHardwareVerification: true, outputService);
  }

  /// <summary>
  /// Проверяет ответ на отключение точки МКР.
  /// </summary>
  /// <param name="response">JSON-ответ МКР.</param>
  /// <param name="module">Модуль, которому отправлена команда.</param>
  /// <param name="pointNumber">Номер отключаемой точки.</param>
  /// <param name="busNumber">Номер шины отключения.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <returns><see langword="true"/>, если ответ соответствует отправленной команде.</returns>
  public static Task<bool> CheckPointDisconnectionAsync(
    string response,
    IRelaySwitchModule module,
    int pointNumber,
    int busNumber,
    IMessageOutputService? outputService = null)
  {
    return CheckPointOperationAsync(response, module, pointNumber, busNumber, connect: false,
      useHardwareVerification: false, outputService);
  }

  /// <summary>
  /// Проверяет ответ на отключение точки МКР с аппаратным контролем.
  /// </summary>
  /// <param name="response">JSON-ответ МКР.</param>
  /// <param name="module">Модуль, которому отправлена команда.</param>
  /// <param name="pointNumber">Номер отключаемой точки.</param>
  /// <param name="busNumber">Номер шины отключения.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <returns><see langword="true"/>, если ответ соответствует команде и состояние реле подтверждено.</returns>
  public static Task<bool> CheckVerifiedPointDisconnectionAsync(
    string response,
    IRelaySwitchModule module,
    int pointNumber,
    int busNumber,
    IMessageOutputService? outputService = null)
  {
    return CheckPointOperationAsync(response, module, pointNumber, busNumber, connect: false,
      useHardwareVerification: true, outputService);
  }

  /// <summary>
  /// Проверяет ответ самоконтроля точки МКР и публикует его результат в экранный протокол.
  /// </summary>
  /// <param name="response">JSON-ответ МКР.</param>
  /// <param name="module">Модуль, выполнивший самоконтроль.</param>
  /// <param name="pointNumber">Ожидаемый номер проверенной точки.</param>
  /// <param name="userInteractionService">Сервис вывода сообщений и накопления ошибок.</param>
  /// <returns>
  /// <see langword="true"/>, если все этапы самоконтроля точки выполнены успешно.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  public static async Task<bool> CheckPointSelfTestAsync(
    string response,
    IRelaySwitchModule module,
    int pointNumber,
    IUserInteractionService? userInteractionService = null)
  {
    ArgumentNullException.ThrowIfNull(module);

    PointSelfTestResponse? model = DeserializePointSelfTestResponse(response);
    if (model == null ||
        !ModuleResponseIdentityChecker.Check(model, module.NumberChassis, module.Number) ||
        model.NumberPoint != pointNumber)
    {
      await SelfTestMessages.PublishResultAsync(
        "\tОшибка данных!",
        false,
        userInteractionService,
        message: response);
      return false;
    }

    bool isValid = PointSelfTestChecker.Check(
      response,
      module.NumberChassis,
      module.Number,
      pointNumber);

    await SelfTestMessages.PublishResultAsync(
      $"{module.NumberChassis}.{module.Number}.{pointNumber}",
      isValid,
      userInteractionService,
      indentLevel: 1,
      executionErrorMessage: isValid ? null : string.Empty,
      executionError: !isValid,
      canBeDeleted: isValid);

    if (isValid)
    {
      return true;
    }

    if (userInteractionService != null)
    {
      int lastLine = userInteractionService.GetLastLineNumber();
      userInteractionService.AddError(ModuleRelayControlError.PointError(
        lastLine,
        $"{module.NumberChassis}.{model.NumberDevice}.{model.NumberPoint}"));
    }

    await SelfTestMessages.PublishResultAsync(
      "Подключение точки",
      model.ConnectPoint,
      userInteractionService,
      indentLevel: 2,
      executionErrorMessage: model.ConnectPoint ? string.Empty : $"Точка[{pointNumber}] - Подключение точки",
      canBeDeleted: model.ConnectPoint);
    await SelfTestMessages.PublishResultAsync(
      "\t\tОтключение с шины А",
      model.DisconnectBusA,
      userInteractionService,
      indentLevel: 2,
      executionErrorMessage: model.DisconnectBusA ? string.Empty : $"Точка[{pointNumber}] - Отключение с шины A",
      canBeDeleted: model.DisconnectBusA);
    await SelfTestMessages.PublishResultAsync(
      "\t\tОтключение с шины B",
      model.DisconnectBusB,
      userInteractionService,
      indentLevel: 2,
      executionErrorMessage: model.DisconnectBusB ? string.Empty : $"Точка[{pointNumber}] - Отключение с шины B",
      canBeDeleted: model.DisconnectBusB);

    return false;
  }

  /// <summary>
  /// Проверяет ответ самоконтроля внешней шины МКР и публикует результат.
  /// </summary>
  /// <param name="response">JSON-ответ МКР.</param>
  /// <param name="module">Модуль, выполнивший самоконтроль.</param>
  /// <param name="busNumber">Ожидаемый номер проверенной внешней шины.</param>
  /// <param name="userInteractionService">Сервис вывода сообщений.</param>
  /// <returns>
  /// <see langword="true"/>, если защитные и основные реле шины прошли самоконтроль.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  public static async Task<bool> CheckExternalBusSelfTestAsync(
    string response,
    IRelaySwitchModule module,
    int busNumber,
    IUserInteractionService? userInteractionService = null)
  {
    ArgumentNullException.ThrowIfNull(module);

    ExternalBusSelfTestResponse? model = ExternalBusSelfTestChecker.Deserialize(response);
    if (!ExternalBusSelfTestChecker.MatchesRequest(
          model,
          module.NumberChassis,
          module.Number,
          busNumber))
    {
      await SelfTestMessages.PublishResultAsync(
        "\tОшибка данных!",
        false,
        userInteractionService,
        message: response);
      return false;
    }

    ExternalBusSelfTestResponse validModel = model!;
    bool isValid = ExternalBusSelfTestChecker.Check(
      response,
      module.NumberChassis,
      module.Number,
      busNumber);

    await SelfTestMessages.PublishResultAsync(
      $"Шины AB{busNumber}",
      isValid,
      userInteractionService,
      indentLevel: 2,
      executionError: !isValid,
      canBeDeleted: isValid);

    if (isValid)
    {
      return true;
    }

    await SelfTestMessages.PublishResultAsync(
      $"\t\tПодключение защитных реле({validModel.ProtectRelayBusA},{validModel.ProtectRelayBusB})",
      validModel.ProtectRelaysConnected,
      userInteractionService,
      indentLevel: 3,
      canBeDeleted: validModel.ProtectRelaysConnected);
    await SelfTestMessages.PublishResultAsync(
      $"\t\tПодключение основных реле({validModel.MainRelayBusA},{validModel.MainRelayBusB})",
      validModel.MainRelaysConnected,
      userInteractionService,
      indentLevel: 3,
      canBeDeleted: validModel.MainRelaysConnected);

    return false;
  }

  private static async Task<bool> CheckPointOperationAsync(
    string response,
    IRelaySwitchModule module,
    int pointNumber,
    int busNumber,
    bool connect,
    bool useHardwareVerification,
    IMessageOutputService? outputService)
  {
    ArgumentNullException.ThrowIfNull(module);

    bool isValid = PointConnectionResponseChecker.Check(
      response,
      module.NumberChassis,
      module.Number,
      pointNumber,
      busNumber,
      connect,
      useHardwareVerification);

    await EquipmentMessages.PublishPointOperationResultAsync(
      module,
      pointNumber,
      (BusPoint)busNumber,
      connect,
      isValid,
      outputService);

    return isValid;
  }

  private static string GetOperationName(string command)
  {
    string[] parts = command.Split('.');
    if (!int.TryParse(parts.ElementAtOrDefault(0), out int commandNumber))
    {
      return "Выполнение команды";
    }

    int.TryParse(parts.ElementAtOrDefault(1), out int firstParameter);
    int.TryParse(parts.ElementAtOrDefault(2), out int secondParameter);
    int.TryParse(parts.ElementAtOrDefault(3), out int thirdParameter);

    return commandNumber switch
    {
      1 => "Инициализация",
      2 => "Сброс",
      4 => thirdParameter == 2 ? "Отключение шины" : "Подключение шины",
      5 => firstParameter == 2 ? "Отключение измерителя" : "Подключение измерителя",
      6 => "Самоконтроль точки",
      7 => "Проверка измерителя",
      8 or 82 => thirdParameter == 2 ? "Отключение точки" : "Подключение точки",
      9 => secondParameter == 2 ? "Отключение точек от шины" : "Подключение точек к шине",
      10 => "Самоконтроль внешней шины",
      11 => thirdParameter % 10 == 2 ? "Отключение диапазона точек" : "Подключение диапазона точек",
      81 => "Переподключение точки",
      _ => $"Выполнение команды {commandNumber}",
    };
  }

  private static PointSelfTestResponse? DeserializePointSelfTestResponse(string response)
  {
    if (string.IsNullOrWhiteSpace(response))
    {
      return null;
    }

    try
    {
      return JsonSerializer.Deserialize<PointSelfTestResponse>(response);
    }
    catch (JsonException)
    {
      return null;
    }
  }
}
