using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Ok;

namespace ControlCommandAnalyser.Parser.KSC
{
  public class KscCommandParser : ICommandParser
  {
    public bool CanParse(string mnemonic) => mnemonic == "КЦ";

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines, RmCommandModel rmCommandModel)
    {
      var model = new KscCommandModel
      {
        CommandNumber = commandNumber,
        Mnemonic = mnemonic,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      return model;
    }
  }
}
