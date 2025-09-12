using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Ok;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Ok;
using NewCore.Base.Interface.Main;
using Utilities;

namespace ControlCommandAnalyser.Parser.KSC
{
  public class KscCommandParser : ICommandParser
  {
    public bool CanParse(string mnemonic) => mnemonic == "КЦ";

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      LoggerUtility.LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");

      var okCommandModel = new OkCommandModel();
      var commandModel = CommandsModel.GetByMnemonic("ОК").Last();

      if (commandModel == null)
      {
        throw new Exception("ОК не сущесвует...");
      }
      else
      {
        if (commandModel is OkCommandModel)
        {
          okCommandModel = commandModel as OkCommandModel;
        }
        else
        {
          throw new Exception("ОК не сущесвует...");
        }
      }

      var model = new KscCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
        OkCommandModel = okCommandModel,
      };

      return model;
    }
  }
}
