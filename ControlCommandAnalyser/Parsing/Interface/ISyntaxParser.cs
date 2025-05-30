using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ControlCommandAnalyser.Parsing.Commands;

namespace ControlCommandAnalyser.Parsing.Interface
{
  public interface ISyntaxParser
  {
    string ParameterName { get; }
    Color HighlightColor { get; }
    SyntaxParseResult? Parse(string line, int lineNumber);
  }
}
