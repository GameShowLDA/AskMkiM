using System.Globalization;
using System.Text.RegularExpressions;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr
{
  /// <summary>
  /// Класс ResistanceParser.
  /// </summary>
  public class ResistanceParser
  {
    /// <summary>
    /// Извлекает выражение сопротивления вида:
    /// "R &lt; 20 МОм", "100 &lt;= МОм" и т. п.
    /// </summary>
    /// <param name="input">Входная строка.</param>
    /// <returns>
    /// Кортеж:
    /// - Value — числовое значение сопротивления или null,  
    /// - Unit — единица измерения (Ом, кОм, МОм, ГОм),  
    /// - Operator — знак сравнения (&lt;, &lt;=, &gt;, &gt;=, =, ≤, ≥),  
    /// - Remainder — остаток строки без выражения.
    /// </returns>
    public (string? Value, string? Unit, string Remainder) ParseResistance(string input)
    {
      if (string.IsNullOrWhiteSpace(input))
        return (null, null, input);

      // Вариант 1: "R < 20 МОм" / "R<=20МОм" (R или русская 'Р')
      var m = Regex.Match(input,
          @"(?<!\w)[RР]\s*(?<op><=|>=|<|>|=|≤|≥)\s*(?<val>\d+(?:[.,]\d+)?)\s*(?<unit>Ом|кОм|МОм|ГОм)\b",
          RegexOptions.IgnoreCase);
      if (m.Success)
      {
        return ParseSiHigherLimitR(input, m);
      }

      // Вариант 2: "100 < МОм" / "100 <= МОм" и т.п.
      m = Regex.Match(input,
          @"(?<!\w)(?<val>\d+(?:[.,]\d+)?)\s*(?<op><=|>=|<|>|=|≤|≥)\s*(?<unit>Ом|кОм|МОм|ГОм)\b",
          RegexOptions.IgnoreCase);
      if (m.Success)
      {
        return ParseSiLowerLimit(input, m);
      }

      return (null, null, input);
    }

    private (string? Value, string? Unit, string Remainder) ParseSiLowerLimit(string input, Match m)
    {
      string unit = m.Groups["unit"].Value;
      double? value = UnitsConvertor.TryParseValue(m.Groups["val"].Value, unit);
      string remainder = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);
      return (value?.ToString("G", CultureInfo.InvariantCulture), "Ом", remainder);
    }

    private (string? Value, string? Unit, string Remainder) ParseSiHigherLimitR(string input, Match m)
    {
      string unit = m.Groups["unit"].Value;
      double? value = UnitsConvertor.TryParseValue(m.Groups["val"].Value, unit);
      string op = m.Groups["op"].Value;
      string remainder = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);
      return (value?.ToString("G", CultureInfo.InvariantCulture), "Ом", remainder);
    }

    /// <summary>
    /// Парсит выражения сопротивлений вида:
    /// "94&lt;кОм&lt;106", "94&lt;кОм", "кОм&lt;106".
    /// </summary>
    /// <param name="input">Входная строка.</param>
    /// <returns>
    /// Кортеж:
    /// - Min — минимальное значение сопротивления,  
    /// - Max — максимальное значение сопротивления,  
    /// - Unit — единица измерения (Ом, кОм, МОм, ГОм),  
    /// - Remainder — остаток строки без выражения.
    /// </returns>
    public (string? Min, string? Max, string? Unit, string Remainder) ParseResistanceRange(string input)
    {
      if (string.IsNullOrWhiteSpace(input))
        return (null, null, null, input);

      var m = Regex.Match(input,
          @"(?:(?<low>\d+(?:[.,]\d+)?(?![.,]\d))\s*<\s*)?(?<unit>Ом|кОм|МОм|ГОм)(?:\s*<\s*(?<high>\d+(?:[.,]\d+)?(?![.,]\d)))?",
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
        minValue?.ToString("G", CultureInfo.InvariantCulture),
        maxValue?.ToString("G", CultureInfo.InvariantCulture),
        "Ом",
        remainder
      );
    }

    /// <summary>
    /// Парсит выражения сопротивлений с использованием символа R:
    /// "10 Ом &lt; R &lt; 20 Ом", "R &lt; 10 МОм", "10 МОм &lt; R".
    /// </summary>
    /// <param name="input">Входная строка.</param>
    /// <returns>
    /// Кортеж:
    /// - Min — минимальное значение (если есть),  
    /// - Max — максимальное значение (если есть),  
    /// - Unit — единица измерения сопротивления,  
    /// - Remainder — остаток строки.
    /// </returns>
    public (string? Resistance, string? Unit, string Remainder) ParseCabelResistance(string input)
    {
      if (string.IsNullOrWhiteSpace(input))
        return (null, null, input);

      // число (с точкой или запятой), затем опциональный пробел и единица
      var m = Regex.Match(input,
          @"(?<val>\d+(?:[.,]\d+)?)\s*(?<unit>Ом|кОм|МОм|ГОм)\b",
          RegexOptions.IgnoreCase);

      if (m.Success)
      {
        string valueText = m.Groups["val"].Value.Replace(',', '.');
        string unit = m.Groups["unit"].Value;

        double? value = UnitsConvertor.TryParseValue(valueText, unit);
        string remainder = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);

        return (
          value?.ToString("G", CultureInfo.InvariantCulture),
          "Ом",
          remainder.Trim()
        );
      }

      return (null, null, input);
    }

    /// <summary>
    /// Парсит выражения сопротивлений с использованием символа R:
    /// "10 Ом &lt; R &lt; 20 Ом", "R &lt; 10 МОм", "10 МОм &lt; R".
    /// </summary>
    /// <param name="input">Входная строка.</param>
    /// <returns>
    /// Кортеж:
    /// - Min — минимальное значение (если есть),  
    /// - Max — максимальное значение (если есть),  
    /// - Unit — единица измерения сопротивления,  
    /// - Remainder — остаток строки.
    /// </returns>
    public (string? Min, string? Max, string? Unit, string Remainder) ParseResistanceRangeWithR(string input)
    {
      if (string.IsNullOrWhiteSpace(input))
        return (null, null, null, input);

      const string Number = @"\d+(?:[.,](?!\d+\.\d)\d+)?";

      // 1) "10 Ом < R < 20 Ом"
      var m = Regex.Match(input,
          $@"(?<!\w)
        (?<low>{Number})\s*(?<unit1>Ом|кОм|МОм|ГОм)?\s*
        <\s*[RР]\s*<\s*
        (?<high>{Number})\s*(?<unit2>Ом|кОм|МОм|ГОм)?",
          RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

      if (m.Success)
        return ParseBothLimitsR(input, m);

      // 2) "R < 10 Ом" / "10 Ом > R"
      m = Regex.Match(input,
          $@"(?<!\w)(?:
            (?<rLeft>[RР])\s*(?<op><=|>=|<|>|≤|≥)\s*(?<val>{Number})\s*(?<unit>Ом|кОм|МОм|ГОм)
            |
            (?<val>{Number})\s*(?<unit>Ом|кОм|МОм|ГОм)\s*(?<op><=|>=|<|>|≤|≥)\s*(?<rRight>[RР])
        )\b",
          RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

      if (m.Success)
        return ParseLimitRUnified(input, m);

      // 4) "10<МОм<20"
      m = Regex.Match(input,
          $@"(?<!\w)
        (?<min>{Number})\s*
        (?<op1><=|>=|<|>|≤|≥)\s*
        (?<unit>Ом|кОм|МОм|ГОм)\s*
        (?<op2><=|>=|<|>|≤|≥)\s*
        (?<max>{Number})\b",
          RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

      if (m.Success)
        return ParseRange(input, m);

      // 5) "Ом < 10"
      m = Regex.Match(input,
          $@"(?<!\w)
        (?<unit>Ом|кОм|МОм|ГОм)\s*
        (?<op><=|>=|<|>|≤|≥)\s*
        (?<val>{Number})\b",
          RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

      if (m.Success)
        return ParseLowerLimit(input, m);

      // 6) "10 < Ом"
      m = Regex.Match(input,
          $@"(?<!\w)
        (?<val>{Number})\s*
        (?<op><=|>=|<|>|≤|≥)\s*
        (?<unit>Ом|кОм|МОм|ГОм)\b",
          RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

      if (m.Success)
        return ParseHigherLimit(input, m);

      return (null, null, null, input);
    }

    private (string? Min, string? Max, string? Unit, string Remainder) ParseLimitRUnified(string input, Match m)
    {
      var op = m.Groups["op"].Value;
      var unit = m.Groups["unit"].Value;

      var value = UnitsConvertor.TryParseValue(
          m.Groups["val"].Value,
          unit
      );

      double? minValue = null;
      double? maxValue = null;

      bool rOnLeft = m.Groups["rLeft"].Success;
      bool rOnRight = m.Groups["rRight"].Success;

      if (rOnLeft)
      {
        // R < 10
        if (op is "<" or "<=" or "≤")
          maxValue = value;
        else
          minValue = value;
      }
      else if (rOnRight)
      {
        // 10 < R
        if (op is "<" or "<=" or "≤")
          minValue = value;
        else
          maxValue = value;
      }

      var remainder = RemoveMatchedWithNeighborComma(
          input,
          m.Index,
          m.Length
      );

      return (
          minValue?.ToString("G", CultureInfo.InvariantCulture),
          maxValue?.ToString("G", CultureInfo.InvariantCulture),
          "Ом",
          remainder
      );
    }

    private (string? Min, string? Max, string? Unit, string Remainder) ParseHigherLimit(string input, Match m)
    {
      var op = m.Groups["op"].Value;
      double? minValue = null, maxValue = null;
      string? unit = m.Groups["unit"].Value;
      if (op is "<" or "<=" or "≤")
      {
        minValue = UnitsConvertor.TryParseValue(m.Groups["val"].Value, unit);
      }
      else
      {
        maxValue = UnitsConvertor.TryParseValue(m.Groups["val"].Value, unit);
      }
      var remainder = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);
      return (
         minValue?.ToString("G", CultureInfo.InvariantCulture),
         maxValue?.ToString("G", CultureInfo.InvariantCulture),
         "Ом",
         remainder
         );
    }

    private (string? Min, string? Max, string? Unit, string Remainder) ParseLowerLimit(string input, Match m)
    {
      var op = m.Groups["op"].Value;
      double? minValue = null, maxValue = null;
      string? unit = m.Groups["unit"].Value;
      if (op is "<" or "<=" or "≤")
      {
        maxValue = UnitsConvertor.TryParseValue(m.Groups["val"].Value, unit);
      }
      else
      {
        minValue = UnitsConvertor.TryParseValue(m.Groups["val"].Value, unit);
      }
      var remainder = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);
      return (
         minValue?.ToString("G", CultureInfo.InvariantCulture),
         maxValue?.ToString("G", CultureInfo.InvariantCulture),
         "Ом",
         remainder
         );
    }

    private (string? Min, string? Max, string? Unit, string Remainder) ParseRange(string input, Match m)
    {
      string? unit = m.Groups["unit"].Value;
      double? maxValue = UnitsConvertor.TryParseValue(m.Groups["max"].Value, unit);
      double? minValue = UnitsConvertor.TryParseValue(m.Groups["min"].Value, unit);
      var remainder = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);
      return (
         minValue?.ToString("G", CultureInfo.InvariantCulture),
         maxValue?.ToString("G", CultureInfo.InvariantCulture),
         "Ом",
         remainder
         );
    }

    private (string? Min, string? Max, string? Unit, string Remainder) ParseBothLimitsR(string input, Match m)
    {
      var unit = m.Groups["unit2"].Success && !string.IsNullOrEmpty(m.Groups["unit2"].Value)
                       ? m.Groups["unit2"].Value
                       : m.Groups["unit1"].Value;
      double? minValue = UnitsConvertor.TryParseValue(m.Groups["low"].Value, unit);
      double? maxValue = UnitsConvertor.TryParseValue(m.Groups["high"].Value, unit);

      var remainder1 = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);
      return (
         minValue?.ToString("G", CultureInfo.InvariantCulture),
         maxValue?.ToString("G", CultureInfo.InvariantCulture),
         "Ом",
         remainder1
         );
    }

    /// <summary>
    /// Вспомогательный метод для удаления найденного фрагмента
    /// и прилегающей запятой с пробелами.
    /// </summary>
    private string RemoveMatchedWithNeighborComma(string input, int index, int length)
    {
      var left = input.Substring(0, index);
      var right = input.Substring(index + length);

      // Сначала попробуем удалить запятую после найденного блока
      right = Regex.Replace(right, @"^\s*,\s*", "");

      // Если справа не было запятой — удалим запятую слева (если она есть)
      if (right == input.Substring(index + length))
        left = Regex.Replace(left, @"\s*,\s*$", "");

      return (left + right).Trim();
    }
  }
}
