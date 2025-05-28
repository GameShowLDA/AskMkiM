using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using ControlCommandAnalyser.Constants;
using ControlCommandAnalyser.Parsing.Interface;

namespace ControlCommandAnalyser.Parsing.Commands
{
  [CommandSyntax("СИ")]
  public class VoltageParser : ISyntaxParser
  {
    private readonly string _pattern;

    public string ParameterName => "Voltage";
    public Color HighlightColor => Colors.Gold;

    public VoltageParser()
    {
      string unitGroup = string.Join("|", KnownUnits.VoltageUnits.Select(Regex.Escape));
      _pattern = $@"\b(\d{{2,4}})({unitGroup})\b";
    }

    public SyntaxParseResult? Parse(string line, int lineNumber)
    {
      var match = Regex.Match(line, @"\b\d{2,4}(В|v|мВ|кВ|MV|KV)\b", RegexOptions.IgnoreCase);
      if (!match.Success) return null;

      return new SyntaxParseResult
      {
        LineIndex = lineNumber,
        Start = match.Index,
        Length = match.Length,
        Target = HighlightTarget.Parameter,
        Color = HighlightColor,
        Description = $"Найдено напряжение: {match.Value}"
      };
    }
  }
}
