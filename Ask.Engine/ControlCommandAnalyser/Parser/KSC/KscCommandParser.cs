using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.KSC
{
  /// <summary>
  /// Парсер команды КЦ.
  /// Создаёт модель команды и связывает её с последней
  /// найденной командой ОК.
  /// </summary>
  public class KscCommandParser : ICommandParser
  {
    /// <summary>
    /// Определяет, может ли парсер обработать указанную мнемонику.
    /// </summary>
    /// <param name="mnemonic">Идентификатор мнемоники.</param>
    /// <returns>
    /// <c>true</c>, если мнемоника соответствует команде КЦ; иначе <c>false</c>.
    /// </returns>
    public bool CanParse(MnemonicIdentifier mnemonic)
    => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.KSC);

    /// <summary>
    /// Выполняет разбор команды КЦ и создаёт модель,
    /// связанную с командой ОК.
    /// </summary>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="mnemonic">Мнемоника команды.</param>
    /// <param name="numberLine">Номер строки начала команды.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <returns>Заполненная модель команды КЦ.</returns>
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
