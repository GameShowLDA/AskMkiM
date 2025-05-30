using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Parsing.Commands
{
  public class SyntaxParseResult
  {
    public int LineIndex { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public HighlightTarget Target { get; set; }
    public System.Windows.Media.Color Color { get; set; }
    public string? Description { get; set; }
  }
}
