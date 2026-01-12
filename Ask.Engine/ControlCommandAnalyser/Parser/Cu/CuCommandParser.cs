using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text.RegularExpressions;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Cu
{
  /// <summary>
  /// Парсер для команд ЦУ.
  /// </summary>
  public class CuCommandParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic)
    => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.CU);

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      var model = new CuCommandModel
      {
        CommandNumber = commandNumber,
        StartLineNumber = numberLine,
        SourceLines = new List<string>(lines)
      };

      List<string> processedLines = CommentsParser.ParseComments(lines, model);
      lines.Clear();
      lines.AddRange(
        processedLines.Count > 0 && processedLines.FindAll(l => string.IsNullOrEmpty(l) || string.IsNullOrWhiteSpace(l)).Count == 0 ?
        processedLines : model.SourceLines
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .ToList()
      );

      // Определяем наличие ключа "Д"
      var firstLine = lines[0].Trim();
      model.IsDocument = Regex.IsMatch(
                         firstLine,
                         @"\bЦУ\s+Д(?!\S)",
                         RegexOptions.IgnoreCase
                         );
      // Для извлечения текста используем оригинальную первую строку
      var textLines = new List<string>();

      // Паттерн: всё после номера и "ЦУ" (и "Д" если есть)
      var pattern = @"^\s*\d+\s+ЦУ(?:\s+Д(?!\S))?\s*(.*)$";

      var match = Regex.Match(firstLine, pattern, RegexOptions.IgnoreCase);
      if (match.Success)
        textLines.Add(match.Groups[1].Value.Trim());
      else
        textLines.Add(firstLine); // fallback, если не подошло

      // Если команда в несколько строк, добавь остальные как есть (кроме первой)
      if (lines.Count > 1)
        textLines.AddRange(lines.Skip(1).Select(l => l.TrimEnd()));

      // Собираем итоговый текст сообщения
      model.MessageText = string.Join(Environment.NewLine, textLines).Trim();

      // Определяем тип команды
      if (model.MessageText.EndsWith("??"))
      {
        model.CuType = CuCommandType.Question;
      }
      else if (model.MessageText.EndsWith("?"))
      {
        model.CuType = CuCommandType.Question;
      }
      else
      {
        model.CuType = CuCommandType.Information;
      }


      return model;
    }
  }
}
