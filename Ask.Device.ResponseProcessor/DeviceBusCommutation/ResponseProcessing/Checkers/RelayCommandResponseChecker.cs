namespace Ask.Device.ResponseProcessor.DeviceBusCommutation.ResponseProcessing.Checkers;

/// <summary>
/// Проверяет числовое подтверждение управления отдельным реле УКШ.
/// </summary>
internal static class RelayCommandResponseChecker
{
  /// <summary>
  /// Проверяет подтверждение управления указанным реле.
  /// </summary>
  /// <param name="response">Числовой ответ УКШ.</param>
  /// <param name="relayNumber">Ожидаемый номер реле.</param>
  /// <returns>
  /// <see langword="true"/>, если ответ содержит ожидаемый номер реле.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  internal static bool Check(string response, int relayNumber)
    => NumericCommandResponseChecker.Check(response, relayNumber);
}
