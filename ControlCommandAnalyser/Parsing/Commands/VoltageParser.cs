using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ControlCommandAnalyser.Constants;

namespace ControlCommandAnalyser.Parsing.Commands
{
  [CommandSyntax("СИ")]
  public class VoltageParser : ISyntaxParser
  {
    private readonly string _pattern;

    public VoltageParser()
    {
      string unitGroup = string.Join("|", KnownUnits.VoltageUnits.Select(Regex.Escape));
      _pattern = $@"\b(\d{{2,4}})({unitGroup})\b";
    }

    public SyntaxParseResult? Parse(string line, int lineNumber)
    {
      var match = Regex.Match(line, _pattern, RegexOptions.IgnoreCase);
      if (!match.Success) return null;

      return new SyntaxParseResult
      {
        LineIndex = lineNumber,
        Start = match.Index,
        Length = match.Length,
        Target = HighlightTarget.Parameter,
        Color = System.Windows.Media.Colors.Gold,
        Description = $"Найдено напряжение: {match.Value}"
      };
    }
  }
}
