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

      block.ExtraHighlights.Clear();
      bool hasVoltage = false;

      // Парсинг напряжения
      var voltageResult = _voltageParser.Parse(line, block.StartLine);
      if (voltageResult != null)
      {
        hasVoltage = true;
        block.ExtraHighlights.Add(new HighlightRange(
            line: voltageResult.LineIndex,
            start: voltageResult.Start,
            length: voltageResult.Length,
            target: voltageResult.Target
        )
        {
          ColorOverride = voltageResult.Color
        });

        LogInformation(voltageResult.Description);
      }

      // Парсинг X<МОМ
      var momResult = _parameterParser.Parse(line, block.StartLine);
      if (momResult != null)
      {
        block.ExtraHighlights.Add(new HighlightRange(
            line: momResult.LineIndex,
            start: momResult.Start,
            length: momResult.Length,
            target: momResult.Target
        )
        {
          ColorOverride = momResult.Color
        });

        LogInformation(momResult.Description);
      }

      block.IsRecognized = hasVoltage;
      await Task.CompletedTask;
    }
  }
}
