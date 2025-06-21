using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Parser.Common;
using AppConfiguration.Error.Translation;
using ControlCommandAnalyser.Model.Ok;

namespace ControlCommandAnalyser.Parser.Si
{
  /// <summary>
  /// Парсер для команд СИ (сопротивление изоляции).
  /// </summary>
  public class SiCommandParser : ICommandParser
  {
    public bool CanParse(string mnemonic) => mnemonic == "СИ";

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      var model = new SiCommandModel
      {
        CommandNumber = commandNumber,
        Mnemonic = mnemonic,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      if (lines == null || lines.Count == 0)
      {
        model.Errors.Add(SiErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
        return model;
      }

      // Первая строка — параметры (напряжение, сопротивление, время)
      var firstLine = lines[0];
      var match = Regex.Match(firstLine, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s*(.*)$");
      if (match.Success)
        firstLine = match.Groups[1].Value.Trim();

      string remainder = firstLine;
      string? voltage = null, resistance = null, time = null;

      // Парсим параметры
      (voltage, remainder) = CommonParameterParser.ParseVoltage(remainder);
      (resistance, remainder) = CommonParameterParser.ParseResistance(remainder);
      (time, remainder) = CommonParameterParser.ParseTime(remainder);

      model.Voltage = voltage;
      model.Resistance = resistance;
      model.Time = time;
      model.UnparsedParameters = remainder;

      if (string.IsNullOrWhiteSpace(voltage))
        model.Errors.Add(SiErrors.CannotParseParameters("Не указано напряжение", numberLine, $"{commandNumber} {mnemonic}"));

      if (string.IsNullOrWhiteSpace(resistance))
        model.Errors.Add(SiErrors.CannotParseParameters("Не указано сопротивление", numberLine, $"{commandNumber} {mnemonic}"));

      if (string.IsNullOrWhiteSpace(time))
        model.Errors.Add(SiErrors.CannotParseParameters("Не указано время", numberLine, $"{commandNumber} {mnemonic}"));

      // Остальные строки — точки
      var points = new List<string>();
      for (int i = 1; i < lines.Count; i++)
      {
        var pointLine = lines[i].Trim();
        if (!string.IsNullOrWhiteSpace(pointLine))
          points.Add(pointLine);
      }
      model.Points = points;

      // Валидация
      if (string.IsNullOrWhiteSpace(voltage) && string.IsNullOrWhiteSpace(resistance) && string.IsNullOrWhiteSpace(time))
        model.Errors.Add(SiErrors.CannotParseParameters(firstLine, numberLine, $"{commandNumber} {mnemonic}"));
      if (points.Count == 0)
        model.Errors.Add(SiErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));

      return model;
    }
  }
}
