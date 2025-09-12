using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Ok;
using NewCore.Base.Interface.Main;

namespace ControlCommandAnalyser.Parser.KSC
{
  public class KscCommandParser : ICommandParser
  {
    public bool CanParse(string mnemonic) => mnemonic == "КЦ";

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      var model = new KscCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      return model;
    }
  }
}
