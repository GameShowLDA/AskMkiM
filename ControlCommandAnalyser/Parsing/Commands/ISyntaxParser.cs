using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Parsing.Commands
{
  public interface ISyntaxParser
  {
    SyntaxParseResult? Parse(string line, int lineNumber);
  }
}
