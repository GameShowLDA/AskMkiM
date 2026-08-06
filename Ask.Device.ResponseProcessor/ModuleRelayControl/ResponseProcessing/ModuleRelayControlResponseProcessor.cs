using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing.Checkers;

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
  /// <param name="chassisNumber">Ожидаемый номер шасси.</param>
  /// <param name="moduleNumber">Ожидаемый номер МКР в шасси.</param>
  /// <param name="pointNumber">Номер подключаемой точки.</param>
  /// <param name="busNumber">Номер шины подключения.</param>
  /// <param name="useHardwareVerification">
  /// Признак использования команды подключения с аппаратным контролем.
  /// </param>
  /// <returns>
  /// <see langword="true"/>, если ответ получен от ожидаемого МКР,
  /// содержит параметры команды подключения и подтверждает требуемое состояние.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  public static bool CheckPointConnection(
    string response,
    int chassisNumber,
    int moduleNumber,
    int pointNumber,
    int busNumber,
    bool useHardwareVerification = false)
  {
    return PointConnectionResponseChecker.Check(
      response,
      chassisNumber,
      moduleNumber,
      pointNumber,
      busNumber,
      connect: true,
      useHardwareVerification);
  }

  /// <summary>
  /// Проверяет ответ на отключение точки МКР.
  /// </summary>
  /// <param name="response">JSON-ответ МКР.</param>
  /// <param name="chassisNumber">Ожидаемый номер шасси.</param>
  /// <param name="moduleNumber">Ожидаемый номер МКР в шасси.</param>
  /// <param name="pointNumber">Номер отключаемой точки.</param>
  /// <param name="busNumber">Номер шины отключения.</param>
  /// <param name="useHardwareVerification">
  /// Признак использования команды отключения с аппаратным контролем.
  /// </param>
  /// <returns>
  /// <see langword="true"/>, если ответ получен от ожидаемого МКР,
  /// содержит параметры команды отключения и подтверждает требуемое состояние.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  public static bool CheckPointDisconnection(
    string response,
    int chassisNumber,
    int moduleNumber,
    int pointNumber,
    int busNumber,
    bool useHardwareVerification = false)
  {
    return PointConnectionResponseChecker.Check(
      response,
      chassisNumber,
      moduleNumber,
      pointNumber,
      busNumber,
      connect: false,
      useHardwareVerification);
  }
}
