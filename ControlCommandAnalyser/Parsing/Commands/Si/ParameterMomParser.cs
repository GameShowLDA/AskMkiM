using System;
using System.Text.RegularExpressions;
using ControlCommandAnalyser.Parsing.Interface;
using System.Windows.Media;

namespace ControlCommandAnalyser.Parsing.Commands.Si
{
  [CommandSyntax("СИ")]
  public class ParameterMomParser : ISyntaxParser
  {
    private readonly string _pattern = @"\b\d+<МОМ\b";

    /// <summary>
    /// Имя параметра для идентификации в парсере команды.
    /// </summary>
    public string ParameterName => "Mom";

    /// <summary>
    /// Цвет подсветки для данного параметра.
    /// </summary>
    public Color HighlightColor => Colors.LightSkyBlue;

    /// <summary>
    /// Выполняет парсинг строки на наличие параметра X<МОМ.
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
        Color = HighlightColor,
        Description = $"Найден параметр X<МОМ: {match.Value}"
      };
    }
  }
}
