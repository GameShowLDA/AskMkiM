using System.Text.RegularExpressions;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers
{
  /// <summary>
  /// Утилитный класс для удаления префикса команды из строки.
  /// </summary>
  internal class TextRemoveManager
  {
    /// <summary>
    /// Удаляет номер и мнемонику команды из начала строки,
    /// если они присутствуют.
    /// </summary>
    /// <param name="remainder">Исходная строка команды.</param>
    /// <returns>Строка без префикса команды.</returns>
    internal static string RemoveCommandPrefix(string remainder)
    {
      var match = Regex.Match(remainder, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s*(.*)$");
      return match.Success ? match.Groups[1].Value.Trim() : remainder;
    }
  }
}
