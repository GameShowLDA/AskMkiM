using System;
using System.Text.RegularExpressions;

namespace ControlCommandAnalyser.Parser.Common
{
  /// <summary>
  /// Предоставляет методы для парсинга общих параметров команд: напряжения, сопротивления и времени.
  /// </summary>
  public static class CommonParameterParser
  {
    public static (string? Voltage, string Remainder) ParseVoltage(string input)
    {
      var match = Regex.Match(input, @"(?<value>\d+\s*(В|кВ|КВ|мВ|МВ))", RegexOptions.IgnoreCase);
      if (match.Success)
      {
        var voltage = match.Groups["value"].Value.Trim();
        var remainder = input.Remove(match.Index, match.Length).Trim(' ', ',');
        return (voltage, remainder);
      }
      return (null, input);
    }

    /// <summary>
    /// Извлекает пороговое сопротивление в формате R&gt;100МОм.
    /// </summary>
    public static (string? ThresholdResistance, string Remainder) ParseThresholdResistance(string input)
    {
      // Совпадает с форматом: R>100МОм или R > 100МОм
      var match = Regex.Match(input, @"R\s*>\s*\d+\s*(Ом|МОм|ГОм)", RegexOptions.IgnoreCase);
      if (match.Success)
      {
        var resistance = match.Value.Trim();
        var remainder = input.Remove(match.Index, match.Length).Trim(' ', ',');
        return (resistance, remainder);
      }
      return (null, input);
    }

    /// <summary>
    /// Старый метод — парсит сопротивление вида "100<МОм" (для СИ).
    /// </summary>
    //public static (string? Resistance, string Remainder) ParseResistance(string input)
    //{
    //  var match = Regex.Match(input, @"(?<value>\d+\s*<\s*(Ом|МОм|ГОм))", RegexOptions.IgnoreCase);
    //  if (match.Success)
    //  {
    //    var resistance = match.Groups["value"].Value.Trim();
    //    var remainder = input.Remove(match.Index, match.Length).Trim(' ');
    //    return (resistance, remainder);
    //  }
    //  return (null, input);
    //}
    public static (string? Resistance, string Remainder) ParseResistance(string input)
    {
      if (string.IsNullOrWhiteSpace(input))
        return (null, input);

      // Вариант 1: "R < 20 МОм" / "R<=20МОм" (R или русская 'Р')
      var m = Regex.Match(input,
          @"(?<!\w)[RР]\s*(?:<=|>=|<|>|=|≤|≥)\s*\d+(?:[.,]\d+)?\s*(?:Ом|кОм|МОм|ГОм)\b",
          RegexOptions.IgnoreCase);
      if (m.Success)
      {
        string resistance = m.Value.Trim();
        string remainder = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);
        return (resistance, remainder);
      }

      // Вариант 2: "100<МОм" / "100 <= МОм" и т.п.
      m = Regex.Match(input,
          @"(?<!\w)\d+(?:[.,]\d+)?\s*(?:<=|>=|<|>|=|≤|≥)\s*(?:Ом|кОм|МОм|ГОм)\b",
          RegexOptions.IgnoreCase);
      if (m.Success)
      {
        string resistance = m.Value.Trim();
        string remainder = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);
        return (resistance, remainder);
      }

      return (null, input);
    }

    /// <summary>
    /// Парсит выражения сопротивлений вида "94&lt;кОм&lt;106", "94&lt;кОм", "кОм&lt;106".
    /// </summary>
    /// <param name="input">Входная строка.</param>
    /// <returns>
    /// Кортеж: 
    /// - Min — минимальное значение сопротивления (если задано),
    /// - Max — максимальное значение сопротивления (если задано),
    /// - Unit — единица измерения сопротивления (Ом, кОм, МОм, ГОм),
    /// - Remainder — остаток строки после удаления выражения.
    /// </returns>
    public static (string? Min, string? Max, string? Unit, string Remainder) ParseResistanceRange(string input)
    {
      var match = Regex.Match(input,
                              @"(?:(?<low>\d+(?:[.,]\d+)?)\s*<\s*)?(?<unit>Ом|кОм|МОм|ГОм)(?:\s*<\s*(?<high>\d+(?:[.,]\d+)?))?",
                              RegexOptions.IgnoreCase);


      if (match.Success)
      {
        string? min = match.Groups["low"].Success ? match.Groups["low"].Value : null;
        string? max = match.Groups["high"].Success ? match.Groups["high"].Value : null;
        string unit = match.Groups["unit"].Value;

        string remainder = input.Remove(match.Index, match.Length).Trim(' ');

        return (min, max, unit, remainder);
      }

      return (null, null, null, input);
    }

    public static (string? Min, string? Max, string? Unit, string Remainder)
  ParseResistanceRangeWithR(string input)
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
        var remainder1 = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);
        return (m.Groups["low"].Value,
                m.Groups["high"].Value,
                string.IsNullOrEmpty(unit) ? "Ом" : unit,
                remainder1);
      }

      // 2) Порог: "R < 10 Ом" или "R <= 10 Ом" (также ≥, >, >=)
      m = Regex.Match(input,
          @"(?<!\w)[RР]\s*(?<op><=|>=|<|>|≤|≥)\s*(?<val>\d+(?:[.,]\d+)?)\s*(?<unit>Ом|кОм|МОм|ГОм)\b",
          RegexOptions.IgnoreCase);
      if (m.Success)
      {
        var op = m.Groups["op"].Value;
        string? min = null, max = null;
        if (op is "<" or "<=" or "≤") max = m.Groups["val"].Value; else min = m.Groups["val"].Value;
        var remainder2 = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);
        return (min, max, m.Groups["unit"].Value, remainder2);
      }

      // 3) Порог: "10 Ом < R" или "10 Ом <= R"
      m = Regex.Match(input,
          @"(?<!\w)(?<val>\d+(?:[.,]\d+)?)\s*(?<unit>Ом|кОм|МОм|ГОм)\s*(?<op><=|>=|<|>|≤|≥)\s*[RР]\b",
          RegexOptions.IgnoreCase);
      if (m.Success)
      {
        var op = m.Groups["op"].Value;
        string? min = null, max = null;
        if (op is "<" or "<=" or "≤") min = m.Groups["val"].Value; else max = m.Groups["val"].Value;
        var remainder3 = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);
        return (min, max, m.Groups["unit"].Value, remainder3);
      }

      // 4) БЕЗ R: "Ом < 10" / "МОм <= 10"  (единица слева, число справа)
      m = Regex.Match(input,
          @"(?<!\w)(?<unit>Ом|кОм|МОм|ГОм)\s*(?<op><=|>=|<|>|≤|≥)\s*(?<val>\d+(?:[.,]\d+)?)\b",
          RegexOptions.IgnoreCase);
      if (m.Success)
      {
        var op = m.Groups["op"].Value;
        string? min = null, max = null;
        if (op is "<" or "<=" or "≤") max = m.Groups["val"].Value; else min = m.Groups["val"].Value;
        var remainder4 = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);
        return (min, max, m.Groups["unit"].Value, remainder4);
      }

      // 5) БЕЗ R: "10 < МОм" / "10 <= МОм"  (число слева, единица справа)
      m = Regex.Match(input,
          @"(?<!\w)(?<val>\d+(?:[.,]\d+)?)\s*(?<op><=|>=|<|>|≤|≥)\s*(?<unit>Ом|кОм|МОм|ГОм)\b",
          RegexOptions.IgnoreCase);
      if (m.Success)
      {
        var op = m.Groups["op"].Value;
        string? min = null, max = null;
        if (op is "<" or "<=" or "≤") min = m.Groups["val"].Value; else max = m.Groups["val"].Value;
        var remainder5 = RemoveMatchedWithNeighborComma(input, m.Index, m.Length);
        return (min, max, m.Groups["unit"].Value, remainder5);
      }

      // Ничего не нашли
      return (null, null, null, input);
    }

    // Удаляет найденный фрагмент + прилегающую запятую и пробелы слева/справа.
    private static string RemoveMatchedWithNeighborComma(string input, int index, int length)
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
    public static (string? Min, string? Max, string? Unit, string Remainder) ParseCapacityRange(string input)
    {
      var match = Regex.Match(input,
                              @"(?:(?<low>\d+(?:[.,]\d+)?)\s*<\s*)?(?<unit>нф|мкф|пф)(?:\s*<\s*(?<high>\d+(?:[.,]\d+)?))?",
                              RegexOptions.IgnoreCase);


      if (match.Success)
      {
        string? min = match.Groups["low"].Success ? match.Groups["low"].Value : null;
        string? max = match.Groups["high"].Success ? match.Groups["high"].Value : null;
        string unit = match.Groups["unit"].Value;

        string remainder = input.Remove(match.Index, match.Length).Trim(' ', ',');

        return (min, max, unit, remainder);
      }

      return (null, null, null, input);
    }
    

    public static (string? Time, string Remainder) ParseTime(string input)
    {
      var match = Regex.Match(input, @"(?:,*\s*(?<value>\d+\s*[сc]))", RegexOptions.IgnoreCase); // TODO: добавить мс

      if (match.Success)
      {
        var time = match.Groups["value"].Value.Trim();
        var remainder = input.Remove(match.Index, match.Length).Trim(' ', ',');
        return (time, remainder);
      }
      return (null, input);
    }
  }
}
