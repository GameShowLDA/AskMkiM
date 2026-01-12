using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Up
{
  /// <summary>
  /// Парсер для команд УП (условный переход).
  /// </summary>
  public class UpCommandParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic)
    => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.UP);

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      var firstLine = lines[0].Trim();

      // После мнемоники сразу идёт номер перехода (метка)
      // Например: "50 УП 1000"
      var parts = firstLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

      string targetLabel = null;

      if (parts.Length >= 3)
      {
        targetLabel = parts[2];
      }
      else if (parts.Length == 2 && lines.Count > 1)
      {
        targetLabel = lines[1].Trim();
      }

      var model = new UpCommandModel
      {
        CommandNumber = commandNumber,
        StartLineNumber = numberLine,
        SourceLines = new List<string>(lines),
        TargetLabel = targetLabel
      };

      List<string> processedLines = CommentsParser.ParseComments(lines, model);
      // Убираем полностью пустые/пробельные строки (чтобы не таскать мусор)
      model.SourceLines = model.SourceLines
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .ToList();

      // Валидация
      if (string.IsNullOrWhiteSpace(targetLabel))
      {
        model.Errors.Add(UpErrors.MissingOrInvalidLabel(numberLine, $"{commandNumber} {mnemonic}"));
      }

      return model;
    }
  }
}
