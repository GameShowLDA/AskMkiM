using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Vsh
{
  public class VshCommandParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic)
    => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.VSH);

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      var model = new VshCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      List<string> processedLines = CommentsParser.ParseComments(lines, model);

      if (lines == null || lines.Count == 0)
      {
        LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
        model.Errors.Add(SiErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
      }

      model = BusStructureParser.ParseVshCommand(string.Join(Environment.NewLine, processedLines), model);

      model.SourceLines = model.SourceLines
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .ToList();

      return model;
    }
  }
}
