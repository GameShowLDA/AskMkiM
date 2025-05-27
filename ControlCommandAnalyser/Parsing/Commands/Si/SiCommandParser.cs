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

      string unitGroup = string.Join("|", KnownUnits.VoltageUnits.Select(Regex.Escape));
      string voltagePattern = $@"\b(\d{{2,4}})({unitGroup})\b";
      string parameterPattern = @"\b\d+<МОМ\b";

      block.ExtraHighlights.Clear();

      var voltageMatch = Regex.Match(line, voltagePattern, RegexOptions.IgnoreCase);
      if (voltageMatch.Success)
      {
        string voltage = voltageMatch.Value;
        LogInformation($"Найдена команда № {block.CommandNumber} СИ — Сопротивление изоляции со значением {voltage} на строке {block.StartLine + 1}");

        block.IsRecognized = true;

        block.ExtraHighlights.Add(new HighlightRange(
            line: block.StartLine,
            start: voltageMatch.Index,
            length: voltageMatch.Length,
            target: HighlightTarget.Parameter
        )
        {
          ColorOverride = Colors.Gold
        });
      }
      else
      {
        LogWarning($"⚠ Команда СИ в строке {block.StartLine + 1} не содержит напряжения.");
        block.IsRecognized = false;

        block.ExtraHighlights.Add(new HighlightRange(
            line: block.StartLine,
            start: line.IndexOf("СИ", StringComparison.OrdinalIgnoreCase),
            length: 2,
            target: HighlightTarget.Mnemonic
        )
        {
          ColorOverride = ShowMessageModel.ErrorMessage.TitleColor
        });
      }

      // Поиск и подсветка X<МОМ
      var parameterMatches = Regex.Matches(line, parameterPattern, RegexOptions.IgnoreCase);
      foreach (Match parameterMatch in parameterMatches)
      {
        block.ExtraHighlights.Add(new HighlightRange(
            line: block.StartLine,
            start: parameterMatch.Index,
            length: parameterMatch.Length,
            target: HighlightTarget.Parameter
        )
        {
          ColorOverride = Colors.LightSkyBlue
        });
      }

      await Task.CompletedTask;
    }

  }
}
