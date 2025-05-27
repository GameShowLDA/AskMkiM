using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using ControlCommandAnalyser.Constants;
using ControlCommandAnalyser.Domain;
using Utilities.Models;
using static Utilities.LoggerUtility;


namespace ControlCommandAnalyser.Parsing.Commands.Si
{
  public class SiCommandParser : ICommandParser
  {
    public string Mnemonic => "СИ";

    public async Task ParseAsync(CommandBlock block)
    {
      var line = block.Lines.FirstOrDefault();
      if (string.IsNullOrWhiteSpace(line))
        return;

      // Собираем шаблон вида: \b(\d{2,4})(В|v|мВ|кВ|MV|KV)\b
      string unitGroup = string.Join("|", KnownUnits.VoltageUnits.Select(Regex.Escape));
      string voltagePattern = $@"\b(\d{{2,4}})({unitGroup})\b";
      var voltageMatch = Regex.Match(line, voltagePattern, RegexOptions.IgnoreCase);

      if (voltageMatch.Success)
      {
        string voltage = voltageMatch.Value;
        LogInformation($"Найдена команда № {block.CommandNumber} СИ — Сопротивление изоляции со значением {voltage} на строке {block.StartLine + 1}");

        block.IsRecognized = true;

        // Подсветка найденного напряжения (например, 100В)
        block.ExtraHighlight = new HighlightRange(
          line: block.StartLine,
          start: voltageMatch.Index,
          length: voltageMatch.Length,
          target: HighlightTarget.Parameter
        )
        {
          ColorOverride = Colors.Gold
        };
      }
      else
      {
        LogWarning($"⚠ Команда СИ в строке {block.StartLine + 1} не содержит напряжения.");
        block.IsRecognized = false;

        // Подсветим саму мнемонику СИ жёлтым как предупреждение
        block.ExtraHighlight = new HighlightRange(
          line: block.StartLine,
          start: line.IndexOf("СИ", StringComparison.OrdinalIgnoreCase),
          length: 2,
          target: HighlightTarget.Mnemonic
        )
        {
          ColorOverride = ShowMessageModel.ErrorMessage.TitleColor
        };
      }

      await Task.CompletedTask;
    }
  }
}
