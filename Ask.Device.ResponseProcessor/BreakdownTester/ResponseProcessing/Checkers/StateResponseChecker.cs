namespace Ask.Device.ResponseProcessor.BreakdownTester.ResponseProcessing.Checkers;

/// <summary>
/// Проверяет ответы состояния пробойной установки.
/// </summary>
internal static class StateResponseChecker
{
  public static bool TryParse(string response, out bool state)
  {
    state = false;
    string normalized = response.Trim().ToUpperInvariant();
    if (normalized == "ON")
    {
      state = true;
      return true;
    }

    return normalized == "OFF";
  }
}
