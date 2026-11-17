using Ask.Core.Services.Config.AppSettings;

namespace Ask.Core.Services.FilesUtility;

public static class ProtocolOutputText
{
  public static string Prepare(string text)
  {
    if (DebugAccessConfig.IsDebugEnabled)
      return text;

    var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
    var visibleLines = lines.Where(line =>
      !line.TrimStart().StartsWith("Версия ПО:", StringComparison.OrdinalIgnoreCase) &&
      !line.TrimStart().StartsWith("Ревизия ПО:", StringComparison.OrdinalIgnoreCase)).ToArray();

    return visibleLines.Length == lines.Length
      ? text
      : string.Join(Environment.NewLine, visibleLines.SkipWhile(string.IsNullOrWhiteSpace));
  }
}
