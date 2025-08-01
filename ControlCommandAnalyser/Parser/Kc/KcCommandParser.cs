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

      var model = new KsCommandModel
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
      LoggerUtility.LogDebug($"После парсинга напряжения: нижняя граница сопртивления='{lowerLimitResistance}',верхняя граница сопртивления='{higherLimitResistance}', единица измерения = '{unit}' remainder='{remainder}'");

      (time, remainder) = CommonParameterParser.ParseTime(remainder);
      LoggerUtility.LogDebug($"После парсинга времени: time='{time}', remainder='{remainder}'");

      if (string.IsNullOrWhiteSpace(lowerLimitResistance)&& string.IsNullOrWhiteSpace(higherLimitResistance))
      {
        model.Errors.Add(KsErrors.EmptyResistance(numberLine, $"{commandNumber} {mnemonic}"));
        LoggerUtility.LogWarning($"Не указано сопротивление (строка {numberLine}): {commandNumber} {mnemonic}");
      }

      model.LowerLimitResistance = lowerLimitResistance;
      model.HigherLimitResistance = higherLimitResistance;
      model.Time = time;

      if(!string.IsNullOrWhiteSpace(model.LowerLimitResistance))
      {
        model.LowerLimitResistance += " " + unit;
      }

      if(!string.IsNullOrWhiteSpace(model.HigherLimitResistance))
      {
        model.HigherLimitResistance += " " + unit;
      }
      var allPoints = new List<string>();
      int starIdx = remainder.IndexOf('*');
      if (starIdx >= 0)
      {
        string pointsPart = remainder.Substring(starIdx);
        LoggerUtility.LogDebug($"Парсинг точек из pointsPart: '{pointsPart}'");
        allPoints.AddRange(PointParser.ParsePoints(pointsPart));
        LoggerUtility.LogInformation($"Найдено точек в pointsPart: {allPoints.Count}");

        remainder = remainder.Substring(0, starIdx).Trim();
      }

      for (int i = 1; i < lines.Count; i++)
      {
        var pointLine = lines[i].Trim();
        if (!string.IsNullOrWhiteSpace(pointLine))
        {
          LoggerUtility.LogDebug($"Парсинг точек из строки {numberLine + i}: '{pointLine}'");
          var pointsBefore = allPoints.Count;
          allPoints.AddRange(PointParser.ParsePoints(pointLine));
          LoggerUtility.LogDebug($"Добавлено точек: {allPoints.Count - pointsBefore}");
        }
      }
      model.Points = allPoints;

      if (!string.IsNullOrEmpty(remainder))
      {
        model.UnparsedParameters = "! Не распознанные параметры: ";
        model.UnparsedParameters += remainder;
        model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{commandNumber} {mnemonic}"));
      }

      // Валидация
      if (string.IsNullOrWhiteSpace(lowerLimitResistance) && string.IsNullOrWhiteSpace(higherLimitResistance) && string.IsNullOrWhiteSpace(time))
      {
        LoggerUtility.LogError($"Не удалось распознать параметры в строке: '{firstLine}' (строка {numberLine})");
        model.Errors.Add(KsErrors.CannotParseParameters(firstLine, numberLine, $"{commandNumber} {mnemonic}"));
      }

      if (model.Points.Count == 0)
      {
        LoggerUtility.LogWarning($"Не найдено ни одной точки (строка {numberLine}): {commandNumber} {mnemonic}");
        model.Errors.Add(KsErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
      }
      else
      {
        LoggerUtility.LogInformation($"Итого найдено точек: {model.Points.Count}");
      }

      AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

      LoggerUtility.LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }
  }
}
