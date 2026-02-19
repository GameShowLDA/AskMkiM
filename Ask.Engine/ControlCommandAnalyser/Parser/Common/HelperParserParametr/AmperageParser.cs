using System.Text.RegularExpressions;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr
{
  public class AmperageParser
  {
    /// <summary>
    /// Парсит выражение напряжения вида "22А", "49.2 мА", "1,5 кА".
    /// </summary>
    /// <param name="input">Входная строка.</param>
    /// <returns>
    /// Кортеж: 
    /// - Value — числовое значение силы тока в виде строки (если найдено),
    /// - Unit — единица измерения силы тока (А, кА, мА, МА),
    /// - Remainder — остаток строки после удаления выражения.
    /// </returns>
    public (string? Value, string? Unit, string Remainder) ParseAmperage(string input)
    {
      var m = Regex.Match(input,
                              @"(?<val>\d+(?:[.,]\d+)?)\s*(?<unit>А|кА|КА|мА|МА)",
                              RegexOptions.IgnoreCase);

      if (!m.Success)
        return (null, null, input);

      string unit = m.Groups["unit"].Value;

      double? value = UnitsConvertor.TryParseValue(m.Groups["val"].Value, unit);

      string remainder = Regex.Replace(
        input,
        $@"\b{Regex.Escape(m.Value)}\s*,?",
        "",
        RegexOptions.IgnoreCase
      ).Trim();

      return (
        value?.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
        "А",
        remainder
      );
    }
  }
}
