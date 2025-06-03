using System.Text.RegularExpressions;

namespace ControlCommandAnalyser.Services
{
  internal static class ParserHelp
  {
    /// <summary>
    /// Ищет и парсит целое число по регулярному выражению (обычно — параметр команды).
    /// </summary>
    private static int TryParseIntParam(string input, string regexPattern)
    {
      var match = Regex.Match(input, regexPattern, RegexOptions.IgnoreCase);
      if (match.Success && int.TryParse(match.Groups[1].Value, out int value))
        return value;
      return -1;
    }

    /// <summary>
    /// Извлекает номер команды из строки блока (например, "30 СИ ...").
    /// Возвращает номер как строку или null, если не найдено.
    /// </summary>
    public static string? TryGetCommandNumber(string line)
    {
      var match = Regex.Match(line, @"^\s*(\d+)\s+\S+", RegexOptions.IgnoreCase);
      return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Извлекает мнемонику команды из строки блока (например, "30 СИ ...").
    /// Возвращает мнемонику как строку или null, если не найдено.
    /// </summary>
    public static string? TryGetMnemonic(string line)
    {
      var match = Regex.Match(line, @"^\s*\d+\s+(\S+)", RegexOptions.IgnoreCase);
      return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Парсит сопротивление, например "100<МОМ"
    /// </summary>
    public static int TryParseResistance(string input)
    {
      return TryParseIntParam(input, @"(\d+)\s*<\s*МОМ");
    }

    /// <summary>
    /// Парсит напряжение, например "100В", "2500В", "5кВ" и т.д.
    /// </summary>
    public static int TryParseVoltage(string input)
    {
      return TryParseIntParam(input, @"(\d+)\s*(В|V|кВ|KV|мВ|MV)");
    }
  }
}
