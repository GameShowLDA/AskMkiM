using System;
using System.Text.RegularExpressions;

namespace ControlCommandAnalyser.Parser.Common
{
  /// <summary>
  /// Предоставляет методы для парсинга общих параметров команд: напряжения, сопротивления и времени.
  /// </summary>
  public static class CommonParameterParser
  {
    /// <summary>
    /// Извлекает значение напряжения из строки.
    /// Примеры: "100В", "2500В", "200кВ", "1 МВ".
    /// </summary>
    /// <param name="input">Исходная строка.</param>
    /// <returns>Кортеж: найденное напряжение (или null) и остаток строки без напряжения.</returns>
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
    /// Извлекает значение сопротивления из строки.
    /// Примеры: "100<МОМ", "200<ОМ", "1<ГОМ".
    /// </summary>
    /// <param name="input">Исходная строка.</param>
    /// <returns>Кортеж: найденное сопротивление (или null) и остаток строки без сопротивления.</returns>
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

    /// <summary>
    /// Извлекает значение времени из строки.
    /// Пример: "1c", "2 c".
    /// </summary>
    /// <param name="input">Исходная строка.</param>
    /// <returns>Кортеж: найденное время (или null) и остаток строки без времени.</returns>
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
