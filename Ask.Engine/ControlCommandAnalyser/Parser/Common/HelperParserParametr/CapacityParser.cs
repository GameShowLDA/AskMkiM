using System.Text.RegularExpressions;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr
{
  public class CapacityParser
  {
    /// <summary>
    /// Парсит выражения ёскости вида "94&lt;мкф&lt;106", "94&lt;мкф", "мкф&lt;106".
    /// </summary>
    /// <param name="input">Входная строка.</param>
    /// <returns>
    /// Кортеж: 
    /// - Min — минимальное значение сопротивления (если задано),
    /// - Max — максимальное значение сопротивления (если задано),
    /// - Unit — единица измерения сопротивления (Ом, кОм, МОм, ГОм),
    /// - Remainder — остаток строки после удаления выражения.
    /// </returns>
    public (string? Min, string? Max, string? Unit, string Remainder) ParseCapacityRange(string input)
    {
      var m = Regex.Match(input,
                              @"(?:(?<low>\d+(?:[.,]\d+)?)\s*<\s*)?(?<unit>нф|мкф|пф)(?:\s*<\s*(?<high>\d+(?:[.,]\d+)?))?",
                              RegexOptions.IgnoreCase);


      if (!m.Success)
        return (null, null, null, input);

      string unit = m.Groups["unit"].Value;

      double? minValue = UnitsConvertor.TryParseValue(m.Groups["low"].Value, unit);
      double? maxValue = UnitsConvertor.TryParseValue(m.Groups["high"].Value, unit);

      string remainder = Regex.Replace(
        input,
        $@"\b{Regex.Escape(m.Value)}\s*,?",
        "",
        RegexOptions.IgnoreCase
      ).Trim();

      return (
        minValue?.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
        maxValue?.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
        "Ф",
        remainder
      );
    }
  }
}
