using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AppConfiguration.Error.Translation;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Parser.Common;
using Utilities;

namespace ControlCommandAnalyser.Parser.Pr
{
  internal class PrCommandParser : ICommandParser
  {
    public bool CanParse(string mnemonic) => mnemonic == "ПР";

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      LoggerUtility.LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");
      var model = new PrCommandModel
      {
        CommandNumber = commandNumber,
        Mnemonic = mnemonic,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      if (lines == null || lines.Count == 0)
      {
        LoggerUtility.LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
        model.Errors.Add(KsErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
        return model;
      }

      var errors = IndentationCheker.CheckIndentationErrors(lines, commandNumber, mnemonic);
      if (errors.Count > 0)
      {
        foreach (var error in errors)
        {
          LoggerUtility.LogError(error);
          model.Errors.Add(GeneralErrors.IndentationError(mnemonic, numberLine, $"{commandNumber} {mnemonic}"));
          return model;
        }
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

      var result = AlgorithmKeyParser.ExtractKeysWithTrailingCommaCheck(firstLine);

      foreach (var (key, hasError) in result)
      {
        if (hasError)
        {
          LoggerUtility.LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
          model.Errors.Add(KsErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
        }
        else
        {
          model.AlgorithmKey.Add(key);
          LoggerUtility.LogDebug($"Найден ключ алгоритма: {key}");
        }
      }

      foreach (var key in model.AlgorithmKey)
      {
        remainder = remainder.Replace(key, "", StringComparison.OrdinalIgnoreCase);
      }

      remainder = remainder.Replace(",","");

      (lowerLimitResistance, higherLimitResistance, unit, remainder) = CommonParameterParser.ParseResistanceRange(remainder);
      LoggerUtility.LogDebug($"После парсинга напряжения: нижняя граница сопртивления='{lowerLimitResistance}',верхняя граница сопртивления='{higherLimitResistance}', единица измерения = '{unit}' remainder='{remainder}'");

      if (string.IsNullOrEmpty(lowerLimitResistance) && string.IsNullOrEmpty(higherLimitResistance))
      {
        model.Errors.Add(KsErrors.EmptyResistance(numberLine, $"{commandNumber} {mnemonic}"));
        LoggerUtility.LogWarning($"Не указано напряжение (строка {numberLine}): {commandNumber} {mnemonic}");
        if (!string.IsNullOrEmpty(remainder))
        {
          model.UnparsedParameters = "! Не распознанные параметры: ";
          model.UnparsedParameters += remainder;
          model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{commandNumber} {mnemonic}"));
        }
        return model;
      }

      model.LowerLimitResistance = lowerLimitResistance;
      model.HigherLimitResistance = higherLimitResistance;

      if (!string.IsNullOrWhiteSpace(model.LowerLimitResistance))
      {
        model.LowerLimitResistance += " " + unit;
      }

      if (!string.IsNullOrWhiteSpace(model.HigherLimitResistance))
      {
        model.HigherLimitResistance += " " + unit;
      }

      if (HasInvalidParameterOrder(firstLine, model.AlgorithmKey, lowerLimitResistance ?? higherLimitResistance, time, out string err))
      {
        model.Errors.Add(GeneralErrors.InvalidParameterOrder(mnemonic, numberLine, $"{commandNumber} {mnemonic}", err));
        LoggerUtility.LogWarning($"Ошибка порядка параметров (строка {numberLine}): {err}");
        return model;
      }

      var allPoints = new List<string>();
      int starIdx = remainder.IndexOf('*');
      if (starIdx >= 0)
      {
        string pointsPart = remainder.Substring(starIdx);
        LoggerUtility.LogDebug($"Парсинг точек из pointsPart: '{pointsPart}'");
        var pointsAndErrors = PointParser.ParsePoints(pointsPart, mnemonic);
        allPoints.AddRange(pointsAndErrors.Item1);
        if (pointsAndErrors.Item2.Count > 0)
        {
          foreach (var error in pointsAndErrors.Item2)
          {
            error.SourceLineNumber = numberLine;
            error.Command = $"{commandNumber} {mnemonic}";

            model.Errors.Add(error);
            LoggerUtility.LogError($"При парсинге точек команды {commandNumber} {mnemonic} произошла ошибка: {error.Description} (строка {error.SourceLineNumber}).");
          }
        }
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
          var pointsAndErrors = PointParser.ParsePoints(pointLine, mnemonic);
          allPoints.AddRange(pointsAndErrors.Item1);
          if (pointsAndErrors.Item2.Count > 0)
          {
            foreach (var error in pointsAndErrors.Item2)
            {
              error.SourceLineNumber = numberLine;
              error.Command = $"{commandNumber} {mnemonic}";
              model.Errors.Add(error);
              LoggerUtility.LogError($"При парсинге точек команды {commandNumber} {mnemonic} произошла ошибка: {error.Description} (строка {error.SourceLineNumber}).");
            }
          }
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

    public static bool HasInvalidParameterOrder(string firstLine, List<string> algorithmKeys, string? resistanceStart, string? time, out string errorDescription)
    {
      errorDescription = string.Empty;

      int idxKey = -1;
      int idxTime = -1;
      int idxResistance = -1;
      int idxPoint = firstLine.IndexOf('*');

      // Позиция первого ключа
      foreach (var key in algorithmKeys)
      {
        int idx = firstLine.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0 && (idxKey == -1 || idx < idxKey))
          idxKey = idx;
      }

      // Время
      if (!string.IsNullOrWhiteSpace(time))
      {
        idxTime = firstLine.IndexOf(time, StringComparison.OrdinalIgnoreCase);
      }

      // Сопротивление
      if (!string.IsNullOrWhiteSpace(resistanceStart))
      {
        idxResistance = firstLine.IndexOf(resistanceStart, StringComparison.OrdinalIgnoreCase);
      }

      // Проверка порядка
      // - Ключ должен идти до времени
      if (idxKey != -1 && idxTime != -1 && idxKey > idxTime)
      {
        errorDescription = "Ключ алгоритма указан после времени.";
        return true;
      }

      // - Ключ должен идти до сопротивления
      if (idxKey != -1 && idxResistance != -1 && idxKey > idxResistance)
      {
        errorDescription = "Ключ алгоритма указан после сопротивления.";
        return true;
      }

      // - Время должно быть после сопротивления
      if (idxTime != -1 && idxResistance != -1 && idxResistance > idxTime)
      {
        errorDescription = "Время указано до сопротивления.";
        return true;
      }

      // - Все параметры должны быть до точек
      if (idxPoint != -1)
      {
        if ((idxKey != -1 && idxKey > idxPoint)
         || (idxTime != -1 && idxTime > idxPoint)
         || (idxResistance != -1 && idxResistance > idxPoint))
        {
          errorDescription = "Один из параметров указан после точек.";
          return true;
        }
      }

      return false;
    }
  }
}
