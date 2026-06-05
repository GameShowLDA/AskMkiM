namespace Ask.Engine.ControlCommandAnalyser.Formatter.Base
{
  internal static class SourceValueFormatter
  {
    public static string Format(
      string? source,
      string title,
      string notSetMessage,
      string? indent = "\t",
      bool showNotSet = true)
    {
      if (!string.IsNullOrWhiteSpace(source))
      {
        return $"{indent}{title}: {source}";
      }

      return showNotSet
        ? $"{indent}{notSetMessage}"
        : string.Empty;
    }
  }
}
