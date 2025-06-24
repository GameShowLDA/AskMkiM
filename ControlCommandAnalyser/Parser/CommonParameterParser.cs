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
        var remainder = input.Remove(match.Index, match.Length).Trim(' ', ',');
        return (resistance, remainder);
      }
      return (null, input);
    }

    public static (string? Time, string Remainder) ParseTime(string input)
    {
      var match = Regex.Match(input, @"(?<value>\d+\s*[сc])", RegexOptions.IgnoreCase);
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
