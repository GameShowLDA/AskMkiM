using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Parser.Common;
using AppConfiguration.Error.Translation;
using ControlCommandAnalyser.Model.Ok;
using Utilities; // Для LoggerUtility

namespace ControlCommandAnalyser.Parser.Kc
{
  /// <summary>
  /// Парсер для команд КС (контроль сопротивления).
  /// </summary>
  [AllowedKeys(AlgorithmKey.Б, AlgorithmKey.Д)]
  internal class KcCommandParser : ICommandParser
  {
    public bool CanParse(string mnemonic) => mnemonic == "КС";


    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      LoggerUtility.LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");

      var model = new KscCommandModel
      {
        CommandNumber = commandNumber,
        Mnemonic = mnemonic,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      if (lines == null || lines.Count == 0)
      {
        LoggerUtility.LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
        model.Errors.Add(SiErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
        return model;
      }

      for (int i = 0; i < model.SourceLines.ToList().Count; i++)
      {
        if (string.IsNullOrEmpty(model.SourceLines[i]) || string.IsNullOrWhiteSpace(model.SourceLines[i]))
        {
          model.SourceLines.Remove(model.SourceLines[i]);
          i--;
        }
      }

      var firstLine = lines[0];
      LoggerUtility.LogDebug($"Исходная первая строка: \"{firstLine}\"");

      var match = Regex.Match(firstLine, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s*(.*)$");
      if (match.Success)
        firstLine = match.Groups[1].Value.Trim();

      string remainder = firstLine;
      string? lowerLimitResistance = null, higherLimitResistance = null, unit = null, time = null;

      // сначала извлекаем ключи
      model.AlgorithmKey = AlgorithmKeyParser.ExtractKeys(firstLine);

      // затем удаляем их из строки
      foreach (var key in model.AlgorithmKey)
      {
        remainder = firstLine.Replace(key, "", StringComparison.OrdinalIgnoreCase);
      }

      (lowerLimitResistance, higherLimitResistance, unit, remainder) = CommonParameterParser.ParseResistanceRange(remainder);
      LoggerUtility.LogDebug($"После парсинга напряжения: voltage='{lowerLimitResistance}', remainder='{remainder}'");

      (time, remainder) = CommonParameterParser.ParseTime(remainder);
      LoggerUtility.LogDebug($"После парсинга времени: time='{time}', remainder='{remainder}'");

      //TODO: создать ошибки для КС

      if (string.IsNullOrWhiteSpace(lowerLimitResistance)&& string.IsNullOrWhiteSpace(higherLimitResistance))
      {
        model.Errors.Add(SiErrors.CannotParseParameters("Не указано напряжение", numberLine, $"{commandNumber} {mnemonic}"));
        LoggerUtility.LogWarning($"Не указано напряжение (строка {numberLine}): {commandNumber} {mnemonic}");
      }

      model.LowerLimitResistance = lowerLimitResistance;
      model.HigherLimitResistance = higherLimitResistance;
      model.Time = time;

      return model;
    }
  }
}
