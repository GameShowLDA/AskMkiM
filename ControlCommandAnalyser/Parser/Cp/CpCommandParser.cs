using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Ok;

namespace ControlCommandAnalyser.Parser.KSC
{
  public class CpCommandParser : ICommandParser
  {
    public bool CanParse(string mnemonic) => mnemonic == "СП";

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines, RmCommandModel rmCommandModel)
    {
      var model = new CpCommandModel
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
