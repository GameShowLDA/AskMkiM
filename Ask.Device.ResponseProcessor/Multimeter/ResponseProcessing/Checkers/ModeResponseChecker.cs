namespace Ask.Device.ResponseProcessor.Multimeter.ResponseProcessing.Checkers;

/// <summary>
/// Проверяет ответ мультиметра на запрос текущего режима.
/// </summary>
internal static class ModeResponseChecker
{
  /// <summary>
  /// Проверяет наличие идентификатора ожидаемого режима в ответе прибора.
  /// </summary>
  /// <param name="response">Ответ мультиметра.</param>
  /// <param name="expectedMode">Ожидаемый идентификатор режима.</param>
  /// <returns>
  /// <see langword="true"/>, если ответ содержит ожидаемый режим.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  internal static bool Check(string response, string expectedMode)
    => !string.IsNullOrWhiteSpace(response) &&
       !string.IsNullOrWhiteSpace(expectedMode) &&
       response.Contains(expectedMode, StringComparison.OrdinalIgnoreCase);
}
