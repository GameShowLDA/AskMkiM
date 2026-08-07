using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing.Checkers;
using Ask.Protocol.Messages.EntryPoints;

namespace Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing;

/// <summary>
/// Предоставляет единую точку входа для обработки ответов МКР.
/// </summary>
public static class ModuleRelayControlResponseProcessor
{
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
}
