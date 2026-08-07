using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing.Checkers;
using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseModels;
using Ask.Protocol.Messages.EntryPoints;
using System.Text.Json;

namespace Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing;

/// <summary>
/// Предоставляет единую точку входа для обработки ответов МКР.
/// </summary>
public static class ModuleRelayControlResponseProcessor
{
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
      $"Точка {pointNumber}",
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
