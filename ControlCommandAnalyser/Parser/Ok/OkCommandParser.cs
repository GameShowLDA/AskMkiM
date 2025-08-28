using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AppConfiguration.Error.Translation;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Ok;

namespace ControlCommandAnalyser.Parser.Ok
{
  /// <summary>
  /// Парсер для команды ОК (объект контроля).
  /// </summary>
  public class OkCommandParser : ICommandParser
  {
    public bool CanParse(string mnemonic) => mnemonic == "ОК";

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      var model = new OkCommandModel
      {
        CommandNumber = commandNumber,
        Mnemonic = mnemonic,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      if (lines == null || lines.Count == 0)
      {
        model.Errors.Add(OkErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
        return model;
      }

      var firstLine = lines[0].Trim();

      // Универсальное регулярное выражение — всегда выдёргиваем номер и мнемонику
      var match = Regex.Match(firstLine, @"^\s*(\d+)\s+ОК(\s+.*)?$", RegexOptions.IgnoreCase);
      if (!match.Success)
      {
        model.Errors.Add(OkErrors.CannotParseFirstLine(numberLine, $"{commandNumber} {mnemonic}", firstLine));
        return model;
      }

      // Остаток после "ОК" (группа 2) может быть пустым
      var mainPart = match.Groups[2].Success ? match.Groups[2].Value.Trim() : string.Empty;

      string objectCode = string.Empty;
      string objectName = null;

      if (string.IsNullOrWhiteSpace(mainPart))
      {
        // Нет обозначения — ошибка
        model.Errors.Add(OkErrors.MissingObjectCode(numberLine, $"{commandNumber} {mnemonic}"));
        model.ObjectCode = string.Empty;
        model.ObjectName = null;
      }
      else
      {
        // Всё что после ОК — это обозначение (наименование не обязательно!)
        if (mainPart.Contains("*"))
        {
          var parts = mainPart.Split(new[] { '*' }, 2, StringSplitOptions.None);
          objectCode = parts[0].Trim();
          objectName = parts.Length > 1 ? parts[1].Trim() : null;
        }
        else
        {
          objectCode = mainPart.Trim();
          objectName = null;
        }

        if (string.IsNullOrWhiteSpace(objectCode))
          model.Errors.Add(OkErrors.MissingObjectCode(numberLine, $"{commandNumber} {mnemonic}"));
        else if (objectCode.Length > 39)
          model.Errors.Add(OkErrors.ObjectCodeTooLong(numberLine, $"{commandNumber} {mnemonic}"));

        model.ObjectCode = objectCode;
        model.ObjectName = objectName;
        // Всё, больше никаких ошибок!
      }


      // --- Параметры (со 2-й строки)
      var uniqueKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      var multiKeys = new HashSet<string> { "КД", "ЦЕХ", "ПРИМ", "ПРИМЕЧ", "ПРИМЕЧАНИЕ" };

      for (int i = 1; i < lines.Count; i++)
      {
        var line = lines[i].Trim();
        if (string.IsNullOrWhiteSpace(line))
          continue;

        var paramMatch = Regex.Match(line, @"^([A-Za-zА-Яа-яёЁ0-9_]+)\s*=\s*(.*)$");
        if (!paramMatch.Success)
        {
          model.Errors.Add(OkErrors.CannotParseParameter(numberLine + i, $"{commandNumber} {mnemonic}", line));
          continue;
        }

        var key = paramMatch.Groups[1].Value.Trim().ToUpperInvariant();
        var value = paramMatch.Groups[2].Value.Trim();

        // Группируем "ПРИМ/ПРИМЕЧ/ПРИМЕЧАНИЕ" как "ПРИМ"
        if (key is "ПРИМЕЧАНИЕ" or "ПРИМЕЧ" or "ПРИМ")
          key = "ПРИМ";

        if (key.Length > 39)
        {
          model.Errors.Add(OkErrors.ParameterKeyTooLong(numberLine + i, $"{commandNumber} {mnemonic}", key));
        }

        int maxLen = key == "ПРИМ" ? 63 : 39;
        if (value.Length > maxLen)
        {
          model.Errors.Add(OkErrors.ParameterValueTooLong(numberLine + i, $"{commandNumber} {mnemonic}", key, maxLen));
        }

        // Проверка уникальности идентификаторов (кроме разрешённых)
        if (!multiKeys.Contains(key))
        {
          if (!uniqueKeys.Add(key))
            model.Errors.Add(OkErrors.DuplicateParameterKey(numberLine + i, $"{commandNumber} {mnemonic}", key));
        }

        // Добавление в Parameters
        if (!model.Parameters.TryGetValue(key, out var list))
        {
          list = new List<string>();
          model.Parameters[key] = list;
        }
        list.Add(value);
      }

      return model;
    }
  }
}
