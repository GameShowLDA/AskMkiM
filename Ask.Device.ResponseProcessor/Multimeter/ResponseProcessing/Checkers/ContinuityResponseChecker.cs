namespace Ask.Device.ResponseProcessor.Multimeter.ResponseProcessing.Checkers;

/// <summary>
/// Проверяет измерительный ответ режима прозвонки мультиметра.
/// </summary>
internal static class ContinuityResponseChecker
{
  /// <summary>
  /// Значение перегрузки, возвращаемое прибором для разомкнутой цепи.
  /// </summary>
  private const double OpenCircuitValue = 9.9E+37;

  /// <summary>
  /// Проверяет соответствие фактического состояния цепи ожидаемому.
  /// </summary>
  /// <param name="response">Измерительный ответ мультиметра.</param>
  /// <param name="expectedClosed">Ожидаемое состояние замкнутой цепи.</param>
  /// <param name="matchesExpected">Результат сравнения состояния цепи.</param>
  /// <returns>
  /// <see langword="true"/>, если ответ корректно обработан.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  internal static bool TryCheck(string response, bool expectedClosed, out bool matchesExpected)
  {
    matchesExpected = false;
    if (!MeasurementResponseChecker.TryParse(response, out var measurement))
    {
      return false;
    }

    bool actualClosed = measurement!.Value < OpenCircuitValue;
    matchesExpected = actualClosed == expectedClosed;
    return true;
  }
}
