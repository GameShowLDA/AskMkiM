namespace Ask.UI.Shared.Formatting
{
  public static class CountDisplayFormatter
  {
    private const int MaxExactCount = 999;

    public static string Format(int count)
      => count > MaxExactCount ? $"{MaxExactCount}+" : count.ToString();

    public static string FormatNonZero(int count)
      => count > 0 ? Format(count) : string.Empty;
  }
}
