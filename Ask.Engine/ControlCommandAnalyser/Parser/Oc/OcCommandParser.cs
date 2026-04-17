using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Oc
{
    /// <summary>
    /// Парсер команды ОС.
    /// </summary>
  public class OcCommandParser : ICommandParser
  {
      /// <summary>
      /// Определяет, может ли парсер обработать указанную мнемонику.
      /// </summary>
      /// <param name="mnemonic">Идентификатор мнемоники.</param>
      /// <returns>
      /// <c>true</c>, если мнемоника соответствует команде ОС; иначе <c>false</c>.
      /// </returns>
      public bool CanParse(MnemonicIdentifier mnemonic)
      => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.OC);

      /// <summary>
      /// Выполняет разбор команды ОС и создаёт модель.
      /// </summary>
      /// <param name="commandNumber">Номер команды.</param>
      /// <param name="mnemonic">Мнемоника команды.</param>
      /// <param name="numberLine">Номер строки начала команды.</param>
      /// <param name="lines">Исходные строки команды.</param>
      /// <returns>Заполненная модель команды ОС.</returns>
      public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
      {
        LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");

        var model = new OcCommandModel
        {
          CommandNumber = commandNumber,
          SourceLines = new List<string>(lines),
          StartLineNumber = numberLine,
        };

        return model;
      }
    }
  }
