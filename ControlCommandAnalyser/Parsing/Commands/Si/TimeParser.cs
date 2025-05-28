using System;
using System.Text.RegularExpressions;

namespace ControlCommandAnalyser.Parsing.Commands.Si
{
  [CommandSyntax("СИ")]
  /// <summary>
  /// Парсер параметра времени Xс (например, 1с, 10с, 500с).
  /// </summary>
  public class TimeParser : ISyntaxParser
  {
    private readonly string _pattern = @"\b\d+[сc]\b";

    /// <summary>
    /// Выполняет парсинг строки на наличие времени Xс.
    /// </summary>
    /// <param name="line">Строка для анализа.</param>
    /// <param name="lineNumber">Номер строки.</param>
    /// <returns>Результат анализа или null, если совпадений нет.</returns>
    public SyntaxParseResult? Parse(string line, int lineNumber)
    {
      var match = Regex.Match(line, _pattern, RegexOptions.IgnoreCase);
      if (!match.Success) return null;

      return new SyntaxParseResult
      {
        LineIndex = lineNumber,
        Start = match.Index,
        Length = match.Length,
        Target = HighlightTarget.Parameter,
        Color = System.Windows.Media.Colors.Gold,
        Description = $"Найден параметр времени: {match.Value}"
      };
    }
  }
}
