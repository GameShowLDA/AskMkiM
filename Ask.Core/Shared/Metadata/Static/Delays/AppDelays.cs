namespace Ask.Core.Shared.Metadata.Static.Delays
{
  /// <summary>
  /// Предоставляет задержки, используемые приложением.
  /// </summary>
  public static class AppDelays
  {
    /// <summary>
    /// Задержки, используемые при работе с пробойной установкой.
    /// </summary>
    public readonly static BreakdownTesterDelays BreakdownTesterDelays;

    /// <summary>
    /// Инициализирует статические задержки приложения.
    /// </summary>
    static AppDelays()
    {
      BreakdownTesterDelays = new BreakdownTesterDelays();
    }
  }
}