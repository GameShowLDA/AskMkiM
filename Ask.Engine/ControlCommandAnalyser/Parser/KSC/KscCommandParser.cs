using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.KSC
{
  public class KscCommandParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic)
    => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.KSC);

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");

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
