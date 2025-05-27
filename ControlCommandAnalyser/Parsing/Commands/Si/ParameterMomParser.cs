using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Parsing.Commands.Si
{
  public class ParameterMomParser : ISyntaxParser
  {
    private readonly string _pattern = @"\b\d+<МОМ\b";

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
        Color = System.Windows.Media.Colors.LightSkyBlue,
        Description = $"Найден параметр X<МОМ: {match.Value}"
      };
    }
  }
}
