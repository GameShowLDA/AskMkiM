using System.Text.RegularExpressions;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Helpers
{
  internal class TextRemoveManager
  {
    internal static string RemoveCommandPrefix(string remainder)
    {
      var match = Regex.Match(remainder, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s*(.*)$");
      return match.Success ? match.Groups[1].Value.Trim() : remainder;
    }
  }
}
