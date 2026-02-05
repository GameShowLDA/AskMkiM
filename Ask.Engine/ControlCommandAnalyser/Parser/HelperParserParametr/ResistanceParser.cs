using System.Text.RegularExpressions;

namespace Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr
{
  public class ResistanceParser
  {
    /// <summary>
    /// Извлекает пороговое сопротивление в формате "R&gt;100МОм".
    /// Поддерживаются варианты "R > X Ом/МОм/ГОм".
    /// </summary>
    /// <param name="input">Входная строка.</param>
    /// <returns>
    /// Кортеж:
    /// - ThresholdResistance — найденное сопротивление или null,  
    /// - Remainder — остаток строки без сопротивления.
    /// </returns>
    public (string? ThresholdResistance, string Remainder) ParseThresholdResistance(string input)
    {
      // Совпадает с форматом: R>100МОм или R > 100МОм
      var match = Regex.Match(input, @"R\s*>\s*\d+\s*(Ом|МОм|ГОм|кОм)", RegexOptions.IgnoreCase);
      if (match.Success)
      {
        var resistance = match.Value.Trim();
        // удаляем найденный фрагмент вместе с запятой и пробелами после него
        var remainder = Regex.Replace(
            input,
            $@"\b{Regex.Escape(match.Value)}\s*,?",
            "",
            RegexOptions.IgnoreCase
        ).Trim();

        return (resistance, remainder);
      }
      return (null, input);
    }
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
        //string value = m.Groups["val"].Value;
        string unit = m.Groups["unit"].Value;
        double? value = UnitsConvertor.TryParseValue(m.Groups["val"].Value, unit);
        string op = m.Groups["op"].Value;
        string remainder = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);
        return (value?.ToString("G", System.Globalization.CultureInfo.InvariantCulture), "Ом", remainder);
      }

      // Вариант 2: "100 < МОм" / "100 <= МОм" и т.п.
      m = Regex.Match(input,
          @"(?<!\w)(?<val>\d+(?:[.,]\d+)?)\s*(?<op><=|>=|<|>|=|≤|≥)\s*(?<unit>Ом|кОм|МОм|ГОм)\b",
          RegexOptions.IgnoreCase);
      if (m.Success)
      {
        string unit = m.Groups["unit"].Value;
        double? value = UnitsConvertor.TryParseValue(m.Groups["val"].Value, unit);
        string remainder = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);
        return (value?.ToString("G", System.Globalization.CultureInfo.InvariantCulture), "Ом", remainder);
      }

      return (null, null, input);
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
          @"(?:(?<low>\d+(?:[.,]\d+)?)\s*<\s*)?(?<unit>Ом|кОм|МОм|ГОм)(?:\s*<\s*(?<high>\d+(?:[.,]\d+)?))?",
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
        string valueText = m.Groups["val"].Value.Replace(',', '.'); // нормализуем запятую
        string unit = m.Groups["unit"].Value;

        double? value = UnitsConvertor.TryParseValue(valueText, unit);
        string remainder = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);

        return (
          value?.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
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

      // 1) Диапазон: "10 Ом < R < 20 Ом"
      var m = Regex.Match(input,
          @"(?<!\w)(?<low>\d+(?:[.,]\d+)?)\s*(?<unit1>Ом|кОм|МОм|ГОм)?\s*<\s*[RР]\b\s*<\s*(?<high>\d+(?:[.,]\d+)?)
        \s*(?<unit2>Ом|кОм|МОм|ГОм)?",
          RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
      if (m.Success)
      {
        var unit = m.Groups["unit2"].Success && !string.IsNullOrEmpty(m.Groups["unit2"].Value)
                 ? m.Groups["unit2"].Value
                 : m.Groups["unit1"].Value;
        double? minValue = UnitsConvertor.TryParseValue(m.Groups["low"].Value, unit);
        double? maxValue = UnitsConvertor.TryParseValue(m.Groups["high"].Value, unit);

        var remainder1 = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);
        return (
           minValue?.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
           maxValue?.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
           "Ом",
           remainder1
           );
      }

      // 2) Порог: "R < 10 Ом" или "R <= 10 Ом" (также ≥, >, >=)
      m = Regex.Match(input,
          @"(?<!\w)[RР]\s*(?<op><=|>=|<|>|≤|≥)\s*(?<val>\d+(?:[.,]\d+)?)\s*(?<unit>Ом|кОм|МОм|ГОм)\b",
          RegexOptions.IgnoreCase);
      if (m.Success)
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
        var remainder2 = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);
        return (
           minValue?.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
           maxValue?.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
           "Ом",
           remainder2
           );
      }

      // 3) Порог: "10 Ом < R" или "10 Ом <= R"
      m = Regex.Match(input,
          @"(?<!\w)(?<val>\d+(?:[.,]\d+)?)\s*(?<unit>Ом|кОм|МОм|ГОм)\s*(?<op><=|>=|<|>|≤|≥)\s*[RР]\b",
          RegexOptions.IgnoreCase);
      if (m.Success)
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
        var remainder3 = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);
        return (
           minValue?.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
           maxValue?.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
           "Ом",
           remainder3
           );
      }

      // 4. Диапазон вида "10<МОм<20"
      m = Regex.Match(input,
          @"(?<!\w)(?<min>\d+(?:[.,]\d+)?)\s*(?<op1><=|>=|<|>|≤|≥)\s*(?<unit>Ом|кОм|МОм|ГОм)\s*(?<op2><=|>=|<|>|≤|≥)\s*(?<max>\d+(?:[.,]\d+)?)\b",
          RegexOptions.IgnoreCase);

      if (m.Success)
      {
        string? unit = m.Groups["unit"].Value;
        double? maxValue = UnitsConvertor.TryParseValue(m.Groups["max"].Value, unit);
        double? minValue = UnitsConvertor.TryParseValue(m.Groups["min"].Value, unit);
        var remainder = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);
        return (
           minValue?.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
           maxValue?.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
           "Ом",
           remainder
           );
      }

      // 5. Нижняя граница 
      m = Regex.Match(input,
          @"(?<!\w)(?<unit>Ом|кОм|МОм|ГОм)\s*(?<op><=|>=|<|>|≤|≥)\s*(?<val>\d+(?:[.,]\d+)?)\b",
          RegexOptions.IgnoreCase);

      if (m.Success)
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
           minValue?.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
           maxValue?.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
           "Ом",
           remainder
           );
      }

      // 6.Верхняя граница
      m = Regex.Match(input,
          @"(?<!\w)(?<val>\d+(?:[.,]\d+)?)\s*(?<op><=|>=|<|>|≤|≥)\s*(?<unit>Ом|кОм|МОм|ГОм)\b",
          RegexOptions.IgnoreCase);

      if (m.Success)
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
           minValue?.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
           maxValue?.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
           "Ом",
           remainder
           );
      }

      return (null, null, null, input);
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
