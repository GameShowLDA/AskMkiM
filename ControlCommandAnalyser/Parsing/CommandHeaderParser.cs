using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Parsing
{
  public static class CommandHeaderParser
  {
    /// <summary>
    /// Пытается извлечь номер команды и мнемонику из строки.
    /// </summary>
    public static bool TryParseHeader(string? line, out string number, out string mnemonic)
    {
      number = "";
      mnemonic = "";

      if (string.IsNullOrWhiteSpace(line))
        return false;

      var match = Regex.Match(line.Trim(), @"^\s*(\d+)\s+(\S+)", RegexOptions.IgnoreCase);
      if (!match.Success)
        return false;

      number = match.Groups[1].Value;
      mnemonic = match.Groups[2].Value.ToUpperInvariant();
      return true;
    }
  }
}
