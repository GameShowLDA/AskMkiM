using System.Text.RegularExpressions;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr
{
  /// <summary>
  /// Класс TimeParser.
  /// </summary>
  public class TimeParser
  {
    /// <summary>
    /// Парсит выражение времени вида "10мс", "200 ms", "5 с".
    /// </summary>
    /// <param name="input">Входная строка.</param>
    /// <returns>
    /// Кортеж: 
    /// - Value — числовое значение времени в виде строки (если найдено),
    /// - Unit — единица измерения времени (мс, ms, с, c),
    /// - Remainder — остаток строки после удаления выражения.
    /// </returns>
    public (string? Value, string? Unit, string Remainder) ParseTime(string input)
    {
      var match = Regex.Match(
          input,
          @"(?:(?<val>\d+(?:[.,]\d+)?)\s*)?(?<unit>(?:м[сc]|ms|с|c))\b",
          RegexOptions.IgnoreCase
          );

      if (match.Success)
      {
        string? value = match.Groups["val"].Success ? match.Groups["val"].Value : null;
        string unit = match.Groups["unit"].Value;

        var remainder = Regex.Replace(
            input,
            $@"\b{Regex.Escape(match.Value)}\s*,?",
            "",
            RegexOptions.IgnoreCase
        ).Trim();

        return (value, unit, remainder);
      }

      return (null, null, input);
    }
  }
}
