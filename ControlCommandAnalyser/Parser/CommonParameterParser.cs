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
    public static (string? Resistance, string Remainder) ParseResistance(string input)
    {
      var match = Regex.Match(input, @"(?<value>\d+\s*<\s*(Ом|МОм|ГОм))", RegexOptions.IgnoreCase);
      if (match.Success)
      {
        var resistance = match.Groups["value"].Value.Trim();
        var remainder = input.Remove(match.Index, match.Length).Trim(' ');
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
      var match = Regex.Match(input, @"(?:,\s*(?<value>\d+\s*[сc]))", RegexOptions.IgnoreCase);

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
