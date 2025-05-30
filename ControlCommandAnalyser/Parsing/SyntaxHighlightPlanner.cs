using System.Collections.Generic;
using ControlCommandAnalyser.Domain;

namespace ControlCommandAnalyser.Parsing
{
  /// <summary>
  /// Построитель подсветки по распознанным строкам команд.
  /// </summary>
  public class SyntaxHighlightPlanner
  {
    /// <summary>
    /// Строит список диапазонов подсветки по разобранным строкам.
    /// </summary>
    /// <param name="parsedLines">Результаты разбора строк.</param>
    /// <returns>Список диапазонов для подсветки.</returns>
    public List<HighlightRange> Build(IEnumerable<ParsedLine> parsedLines)
    {
      var result = new List<HighlightRange>();

      foreach (var line in parsedLines)
      {
        if (!line.HasCommand)
          continue;

        if (line.CommandNumberOffset is int numOffset && line.CommandNumber is { } number)
        {
          result.Add(new HighlightRange(line.LineIndex, numOffset, number.Length, HighlightTarget.CommandNumber));
        }

        if (line.MnemonicOffset is int mnemonicOffset && line.Mnemonic is { } mnemonic)
        {
          result.Add(new HighlightRange(line.LineIndex, mnemonicOffset, mnemonic.Length, HighlightTarget.Mnemonic));
        }
      }

      return result;
    }
  }
}
