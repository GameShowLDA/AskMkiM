using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Rm
{
  public class RmCommandParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic)
    => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.RM);

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      var model = new RmCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      // Собрать весь текст команды (все строки после номера и мнемоники)
      var sb = new System.Text.StringBuilder();
      List<string> processedLines = CommentsParser.ParseComments(lines, model);
      lines.Clear();
      lines.AddRange(processedLines);
      for (int i = 0; i < lines.Count; i++)
      {
        var line = lines[i].Trim();
        if (i == 0)
        {
          // Убрать номер и мнемонику
          var match = System.Text.RegularExpressions.Regex.Match(line, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s*(.*)$");
          if (match.Success) line = match.Groups[1].Value.Trim();
        }
        if (!string.IsNullOrWhiteSpace(line))
        {
          sb.AppendLine(line);
          model.PointsSourse = line;
        }
      }

      var pairs = RmExpressionParser.ParseAllExpressions(sb.ToString(), ref model);

      foreach (var pair in pairs)
        model.PointsMap[pair.OkPoint] = pair.AskInput;


      return model;
    }
  }
}
