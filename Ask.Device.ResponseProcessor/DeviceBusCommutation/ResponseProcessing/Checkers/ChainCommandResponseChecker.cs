namespace Ask.Device.ResponseProcessor.DeviceBusCommutation.ResponseProcessing.Checkers;

/// <summary>
/// Проверяет числовой результат коммутации цепи УКШ.
/// </summary>
internal static class ChainCommandResponseChecker
{
  /// <summary>
  /// Проверяет код результата коммутации цепи.
  /// </summary>
  /// <param name="response">Числовой ответ УКШ.</param>
  /// <returns>
  /// <see langword="true"/>, если УКШ вернул нулевой код успешного выполнения.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  internal static bool Check(string response)
    => NumericCommandResponseChecker.Check(response, 0);
}
