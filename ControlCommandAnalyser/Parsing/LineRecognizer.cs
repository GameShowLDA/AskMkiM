using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;
using ControlCommandAnalyser.Domain;

namespace ControlCommandAnalyser.Parsing
{
  /// <summary>
  /// Распознаёт структуру командной строки формата ПК.
  /// </summary>
  public class LineRecognizer
  {
    private static readonly Regex CommandPattern = new(@"^\s*(\d{1,3})\s+(\S+)", RegexOptions.Compiled);

    /// <summary>
    /// Пытается разобрать строку как команду с номером и мнемоникой.
    /// </summary>
    /// <param name="line">Текст строки.</param>
    /// <param name="index">Номер строки в документе (0-based).</param>
    /// <returns>Структура с распознанными элементами.</returns>
    public ParsedLine Parse(string line, int index)
    {
      var parsed = new ParsedLine
      {
        LineIndex = index,
        Text = line
      };

      var match = CommandPattern.Match(line);
      if (!match.Success)
        return parsed;

      parsed.CommandNumber = match.Groups[1].Value;
      parsed.Mnemonic = match.Groups[2].Value;

      parsed.CommandNumberOffset = match.Groups[1].Index;
      parsed.MnemonicOffset = match.Groups[2].Index;

      return parsed;
    }
  }
}
