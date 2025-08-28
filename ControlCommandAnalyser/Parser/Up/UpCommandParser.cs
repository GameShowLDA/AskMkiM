using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;

namespace ControlCommandAnalyser.Parser.Up
{
  /// <summary>
  /// Парсер для команд УП (условный переход).
  /// </summary>
  public class UpCommandParser : ICommandParser
  {
    public bool CanParse(string mnemonic) => mnemonic == "УП";

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
        Mnemonic = mnemonic,
        StartLineNumber = numberLine,
        SourceLines = new List<string>(lines),
        TargetLabel = targetLabel
      };

      // Валидация
      if (string.IsNullOrWhiteSpace(targetLabel))
      {
        model.Errors.Add(AppConfiguration.Error.Translation.UpErrors.MissingOrInvalidLabel(numberLine, $"{commandNumber} {mnemonic}"));
      }

      return model;
    }
  }
}
