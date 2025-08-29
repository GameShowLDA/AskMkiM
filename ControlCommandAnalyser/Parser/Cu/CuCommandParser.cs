using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;

namespace ControlCommandAnalyser.Parser.Cu
{
  /// <summary>
  /// Парсер для команд ЦУ.
  /// </summary>
  public class CuCommandParser : ICommandParser
  {
    public bool CanParse(string mnemonic) => mnemonic == "ЦУ";

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines, RmCommandModel rmCommandModel)
    {
      var model = new CuCommandModel
      {
        CommandNumber = commandNumber,
        Mnemonic = mnemonic,
        StartLineNumber = numberLine,
        SourceLines = new List<string>(lines)
      };

      // Определяем наличие ключа "Д"
      var firstLine = lines[0].Trim();
      model.IsDocument = Regex.IsMatch(firstLine, @"^\s*Д\s+", RegexOptions.IgnoreCase);

      // Для извлечения текста используем оригинальную первую строку
      var textLines = new List<string>();

      // Паттерн: всё после номера и "ЦУ" (и "Д" если есть)
      var pattern = @"^\s*\d+\s+ЦУ(?:\s+Д)?\s*(.*)$";
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
