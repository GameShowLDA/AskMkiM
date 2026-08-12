using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseModels;

namespace Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing.Checkers;

/// <summary>
/// Проверяет ответы на подключение и отключение одной точки МКР.
/// </summary>
internal static class PointConnectionResponseChecker
{
  private const int PointCommandNumber = 8;
  private const int VerifiedPointCommandNumber = 82;
  private const int ConnectAction = 1;
  private const int DisconnectAction = 2;

  /// <summary>
  /// Проверяет отправителя, параметры команды и аппаратное подтверждение состояния точки.
  /// </summary>
  /// <param name="response">JSON-ответ МКР.</param>
  /// <param name="chassisNumber">Ожидаемый номер шасси.</param>
  /// <param name="moduleNumber">Ожидаемый номер МКР в шасси.</param>
  /// <param name="pointNumber">Ожидаемый номер точки.</param>
  /// <param name="busNumber">Ожидаемый номер шины.</param>
  /// <param name="connect">Ожидаемое действие подключения точки.</param>
  /// <param name="useHardwareVerification">Признак команды с аппаратным контролем.</param>
  /// <returns>
  /// <see langword="true"/>, если ответ полностью соответствует ожидаемой операции.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  internal static bool Check(
    string response,
    int chassisNumber,
    int moduleNumber,
    int pointNumber,
    int busNumber,
    bool connect,
    bool useHardwareVerification)
  {
    int commandNumber = useHardwareVerification
      ? VerifiedPointCommandNumber
      : PointCommandNumber;
    int action = connect ? ConnectAction : DisconnectAction;
    string expectedAnswer = $"{commandNumber}.{pointNumber}.{busNumber}.{action}";

    return CommandResponseChecker.Check(
      response,
      chassisNumber,
      moduleNumber,
      expectedAnswer,
      useHardwareVerification);
  }
}
