using Ask.Core.Services.Extensions;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text.RegularExpressions;
using Ask.Core.Services.Errors.Translation;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Cu
{
  /// <summary>
  /// Парсер команды ЦУ.
  /// Формирует модель сообщения, определяет тип команды
  /// и извлекает текст сообщения.
  /// </summary>
  public class CuCommandParser : ICommandParser
  {
    /// <summary>
    /// Определяет, может ли парсер обработать указанную мнемонику.
    /// </summary>
    /// <param name="mnemonic">Идентификатор мнемоники.</param>
    /// <returns>
    /// <c>true</c>, если мнемоника соответствует команде ЦУ; иначе <c>false</c>.
    /// </returns>
    public bool CanParse(MnemonicIdentifier mnemonic)
    => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.CU);

    /// <summary>
    /// Выполняет разбор команды ЦУ и формирует модель сообщения.
    /// </summary>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="mnemonic">Мнемоника команды.</param>
    /// <param name="numberLine">Номер строки начала команды.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <returns>Заполненная модель команды ЦУ.</returns>
    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      var model = new CuCommandModel
      {
        CommandNumber = commandNumber,
        StartLineNumber = numberLine,
        SourceLines = lines is null ? new List<string>() : new List<string>(lines),
      };

      List<string> processedLines = CommentsParser.ParseComments(lines, model);
      lines.Clear();
      lines.AddRange(
        processedLines.Count > 0 && processedLines.FindAll(l => string.IsNullOrEmpty(l) || string.IsNullOrWhiteSpace(l)).Count == 0 ?
        processedLines : model.SourceLines
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .ToList()
      );

      var firstLine = lines[0].Trim();
      model.IsDocument = Regex.IsMatch(
                         firstLine,
                         @"\bЦУ\s+Д(?!\S)",
                         RegexOptions.IgnoreCase
                         );
      var textLines = new List<string>();

      // Паттерн: всё после номера и "ЦУ" (и "Д" если есть)
      var pattern = @"^\s*\d+\s+ЦУ(?:\s+Д(?!\S))?\s*(.*)$";

      var match = Regex.Match(firstLine, pattern, RegexOptions.IgnoreCase);
      if (match.Success)
      {
        textLines.Add(match.Groups[1].Value.Trim());
      }
      else
      {
        textLines.Add(firstLine); 
      }
      if (lines.Count > 1)
      {
        textLines.AddRange(lines.Skip(1).Select(l => l.TrimEnd()));
      }

      var rawMessageText = string.Join(Environment.NewLine, textLines).Trim();
      model.MessageText = rawMessageText;

      if (string.IsNullOrWhiteSpace(rawMessageText))
      {
        model.Errors.Add(new ErrorItem
        {
          SourceLineNumber = numberLine,
          Command = $"{commandNumber} {mnemonic}",
          Code = ErrorCode.Unknown,
          Description = "После команды ЦУ должен быть указан текст сообщения."
        });
      }

      if (rawMessageText.Contains('?') &&
          !rawMessageText.EndsWith("?") &&
          !rawMessageText.EndsWith("??"))
      {
        model.Warnings.Add(new WarningItem
        {
          SourceLineNumber = numberLine,
          Command = $"{commandNumber} {mnemonic}",
          Code = WarningCode.Unknown,
          Description = "Вопросительный знак в команде ЦУ должен завершать сообщение."
        });
      }

      if (rawMessageText.EndsWith("??"))
      {
        model.CuType = CuCommandType.Question;
      }
      else if (rawMessageText.EndsWith("?"))
      {
        model.CuType = CuCommandType.Question;
      }
      else
      {
        model.CuType = CuCommandType.Information;
      }

      if (model.CuType == CuCommandType.Question)
      {
        model.MessageText = TrimTrailingQuestionMarks(rawMessageText);
      }

      if (string.IsNullOrWhiteSpace(model.MessageText))
      {
        model.Warnings.Add(GeneralWarnings.EmptyMessage(numberLine, $"{commandNumber} {mnemonic}"));
      }

      return model;
    }

    private static string TrimTrailingQuestionMarks(string messageText)
    {
      return messageText.TrimEnd().TrimEnd('?').TrimEnd();
    }
  }
}
