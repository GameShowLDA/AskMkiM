using ControlCommandAnalyser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Parser.Kc
{
  internal class KcCommandParser : ICommandParser
  {
    public bool CanParse(string mnemonic) => mnemonic == "КС";


    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      throw new NotImplementedException();
    }
  }
}
