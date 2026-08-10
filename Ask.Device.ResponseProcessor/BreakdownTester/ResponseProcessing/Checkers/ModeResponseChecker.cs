namespace Ask.Device.ResponseProcessor.BreakdownTester.ResponseProcessing.Checkers;

/// <summary>
/// Проверяет ответ текущего режима пробойной установки.
/// </summary>
internal static class ModeResponseChecker
{
  public static bool Check(string response, string expectedMode)
    => string.Equals(response.Trim(), expectedMode, StringComparison.OrdinalIgnoreCase);
}
