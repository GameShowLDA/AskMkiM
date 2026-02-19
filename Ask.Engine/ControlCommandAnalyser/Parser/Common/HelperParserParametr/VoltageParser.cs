using System.Text.RegularExpressions;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr
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

    /// <summary>
    /// Парсит выражения с диапазонами напряжения.
    /// </summary>
    /// <param name="input">Входная строка.</param>
    /// <returns>
    /// Кортеж:
    /// - Min — минимальное значение (если есть),  
    /// - Max — максимальное значение (если есть),  
    /// - Unit — единица измерения сопротивления,  
    /// - Remainder — остаток строки.
    /// </returns>
    public (string? Min, string? Max, string? Unit, string Remainder) ParseVoltageRange(string input)
    {
      if (string.IsNullOrWhiteSpace(input))
        return (null, null, null, input);

      // 4. Диапазон вида "10<МОм<20"
      var m = Regex.Match(input,
           @"(?<!\w)(?<min>\d+(?:[.,]\d+)?)\s*(?<op1><=|>=|<|>|≤|≥)\s*(?<unit>В|кВ|КВ|мВ|МВ)\s*(?<op2><=|>=|<|>|≤|≥)\s*(?<max>\d+(?:[.,]\d+)?)\b",
           RegexOptions.IgnoreCase);

      if (m.Success)
      {
        string? unit = m.Groups["unit"].Value;
        double? maxValue = UnitsConvertor.TryParseValue(m.Groups["max"].Value, unit);
        double? minValue = UnitsConvertor.TryParseValue(m.Groups["min"].Value, unit);
        string remainder = Regex.Replace(
        input,
        $@"\b{Regex.Escape(m.Value)}\s*,?",
        "",
        RegexOptions.IgnoreCase
      ).Trim();
        return (
           minValue?.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
           maxValue?.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
           "В",
           remainder
           );
      }

      // Ничего не нашли
      return (null, null, null, input);
    }
  }
}
