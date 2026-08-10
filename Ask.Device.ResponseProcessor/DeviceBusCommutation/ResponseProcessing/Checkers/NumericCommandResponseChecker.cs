namespace Ask.Device.ResponseProcessor.DeviceBusCommutation.ResponseProcessing.Checkers;

/// <summary>
/// Проверяет числовой ответ УКШ.
/// </summary>
internal static class NumericCommandResponseChecker
{
  /// <summary>
  /// Проверяет точное числовое значение ответа УКШ.
  /// </summary>
  /// <param name="response">Числовой ответ УКШ.</param>
  /// <param name="expectedValue">Ожидаемое числовое значение.</param>
  /// <returns>
  /// <see langword="true"/>, если ответ является ожидаемым числом.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  public static bool Check(string response, int expectedValue)
    => int.TryParse(response?.Trim(), out int value) && value == expectedValue;

  /// <summary>
  /// Преобразует ответ УКШ в целое число.
  /// </summary>
  /// <param name="response">Числовой ответ УКШ.</param>
  /// <param name="value">Полученное числовое значение.</param>
  /// <returns>
  /// <see langword="true"/>, если ответ успешно преобразован.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  public static bool TryRead(string response, out int value)
    => int.TryParse(response?.Trim(), out value);
}
