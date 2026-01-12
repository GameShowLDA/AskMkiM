using System.Text.RegularExpressions;

namespace Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr
{
  public class VoltageParser
  {
    /// <summary>
    /// Парсит выражение напряжения вида "220В", "49.2 мВ", "1,5 кВ".
    /// </summary>
    /// <param name="input">Входная строка.</param>
    /// <returns>
    /// Кортеж: 
    /// - Value — числовое значение напряжения в виде строки (если найдено),
    /// - Unit — единица измерения напряжения (В, кВ, мВ, МВ),
    /// - Remainder — остаток строки после удаления выражения.
    /// </returns>
    public (string? Value, string? Unit, string Remainder) ParseVoltage(string input)
    {
      var m = Regex.Match(input,
                              @"(?<val>\d+(?:[.,]\d+)?)\s*(?<unit>В|кВ|КВ|мВ|МВ)",
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
        "В",
        remainder
      );
    }
  }
}
