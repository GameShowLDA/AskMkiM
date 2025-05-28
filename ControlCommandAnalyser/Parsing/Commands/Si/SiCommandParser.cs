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

    private readonly VoltageParser _voltageParser = new();
    private readonly ParameterMomParser _parameterParser = new();

    public async Task ParseAsync(CommandBlock block)
    {
      var line = block.Lines.FirstOrDefault();
      if (string.IsNullOrWhiteSpace(line))
        return;

      string unitGroup = string.Join("|", KnownUnits.VoltageUnits.Select(Regex.Escape));
      string voltagePattern = $@"\b(\d{{2,4}})({unitGroup})\b";
      string parameterPattern = @"\b\d+<МОМ\b";
      string timePattern = @"\b\d+[сc]\b";

      block.ExtraHighlights.Clear();

      // Поиск напряжения
      var voltageMatch = Regex.Match(line, voltagePattern, RegexOptions.IgnoreCase);
      bool voltageFound = false;
      if (voltageMatch.Success)
      {
        voltageFound = true;
        block.ExtraHighlights.Add(new HighlightRange(
            line: block.StartLine,
            start: voltageMatch.Index,
            length: voltageMatch.Length,
            target: HighlightTarget.Parameter
        )
        {
          ColorOverride = Colors.Gold
        });

        LogInformation($"Найдено напряжение: {voltageMatch.Value}");
      }

      // Поиск X<МОМ
      var parameterMatches = Regex.Matches(line, parameterPattern, RegexOptions.IgnoreCase);
      bool parameterFound = parameterMatches.Count > 0;
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

        LogInformation($"Найден параметр X<МОМ: {parameterMatch.Value}");
      }

      // Поиск времени Xс (кириллица/латиница)
      var timeMatches = Regex.Matches(line, timePattern, RegexOptions.IgnoreCase);
      bool timeFound = timeMatches.Count > 0;
      foreach (Match timeMatch in timeMatches)
      {
        block.ExtraHighlights.Add(new HighlightRange(
            line: block.StartLine,
            start: timeMatch.Index,
            length: timeMatch.Length,
            target: HighlightTarget.Parameter
        )
        {
          ColorOverride = Colors.Gold
        });

        LogInformation($"Найден параметр времени: {timeMatch.Value}");
      }

      if (voltageFound && parameterFound && timeFound)
      {
        block.IsRecognized = true;
      }
      else
      {
        block.IsRecognized = false;
        int siIndex = line.IndexOf("СИ", StringComparison.OrdinalIgnoreCase);
        if (siIndex >= 0)
        {
          block.ExtraHighlights.Add(new HighlightRange(
              line: block.StartLine,
              start: siIndex,
              length: 2,
              target: HighlightTarget.Mnemonic
          )
          {
            ColorOverride = ShowMessageModel.ErrorMessage.TitleColor
          });
        }

        LogWarning($"⚠ Команда СИ в строке {block.StartLine + 1} не содержит напряжения, параметра X<МОМ или времени.");
      }

      await Task.CompletedTask;
    }

  }
}
