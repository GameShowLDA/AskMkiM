using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;
using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr; // Для LoggerUtility
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Kc
{
  /// <summary>
  /// Парсер для команд КС (контроль сопротивления).
  /// </summary>
  [AllowedKeys(Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Б, Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Д)]
  internal class KcCommandParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic) => mnemonic.Mnemonic.MatchesEnum(MeasurementTypeCommand.KC);

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");

      var model = new KsCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };
      var rmCommandModel = CommandsModel.GetRMModel();

      if (rmCommandModel == null)
      {
        LogError($"Команда РМ не найдена");
        model.Errors.Add(KsErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
      }

      if (lines == null || lines.Count == 0)
      {
        LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
        model.Errors.Add(KsErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
        return model;
      }

      var errors = IndentationCheker.CheckIndentationErrors(lines, commandNumber, mnemonic);
      if (errors.Count > 0)
      {
        foreach (var error in errors)
        {
          LogError(error);
          model.Errors.Add(GeneralErrors.IndentationError(mnemonic, numberLine, $"{commandNumber} {mnemonic}"));
          return model;
        }
      }

      List<string> processedLines = CommentsParser.ParseComments(lines, model);
      model.SourceLines = model.SourceLines
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .ToList();

      var body = string.Concat(processedLines.Count > 0 && processedLines.FindAll(l => string.IsNullOrEmpty(l) || string.IsNullOrWhiteSpace(l)).Count == 0 ?
        processedLines : model.SourceLines)
        .Replace("\r", "")
        .Replace("\n", "")
        .Replace("\t", "");

      LogDebug($"Нормализованное тело команды (в одну строку): \"{body}\"");

      var remainder = body;

      var match = Regex.Match(remainder, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s*(.*)$");
      if (match.Success)
        remainder = match.Groups[1].Value.Trim();

      string? lowerLimitResistance = null, higherLimitResistance = null, unit = null, time = null;

      remainder = KeyParser.ParseKeys(numberLine, model, remainder);

      (lowerLimitResistance, higherLimitResistance, unit, remainder) = CommonParameterParser.ResistanceParser.ParseResistanceRange(remainder);
      LogDebug($"После парсинга сопротивления: нижняя граница='{lowerLimitResistance}', верхняя граница='{higherLimitResistance}', единица='{unit}', remainder='{remainder}'");

      if (string.IsNullOrEmpty(lowerLimitResistance) && string.IsNullOrEmpty(higherLimitResistance))
      {
        model.Errors.Add(KsErrors.EmptyResistance(numberLine, $"{commandNumber} {mnemonic}"));
        LogWarning($"Не указано сопротивление (строка {numberLine}): {commandNumber} {mnemonic}");

        if (!string.IsNullOrEmpty(remainder))
        {
          model.UnparsedParameters = "! Не распознанные параметры: " + remainder;
          model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{commandNumber} {mnemonic}"));
        }

        return model;
      }

      double? lower = !string.IsNullOrWhiteSpace(lowerLimitResistance) ? CommonParameterParser.ParseToDouble(lowerLimitResistance) : null;
      double? higher = !string.IsNullOrWhiteSpace(higherLimitResistance) ? CommonParameterParser.ParseToDouble(higherLimitResistance) : null;

      var meter = new DataBaseConfiguration.Services.Device.FastMeterServices().GetAll().FirstOrDefault();
      if (meter == null)
      {
        LogError($"Не найден быстрый измеритель.");
        model.Errors.Add(GeneralErrors.FastMeterNotFound(numberLine, $"{commandNumber} {mnemonic}"));
        return model;
      }
      else
      {
        var commandInfo = EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.KC);

        double minResistance = commandInfo.LowerLimit;
        double maxResistance = commandInfo.UpperLimit;

        bool hasErrors = false;

        if (lower.HasValue && higher.HasValue)
        {
          if (lower.Value >= higher.Value)
          {
            var lowerValue = UnitsConvertor.TryConvertBack(lower.Value, unit);
            var higherValue = UnitsConvertor.TryConvertBack(higher.Value, unit);
            LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) нижняя граница сопротивления больше или равна верхней.");
            model.Errors.Add(KsErrors.ResistanceLimitsConflict(numberLine, $"{commandNumber} {mnemonic}",
              $"Нижняя граница сопротивления ({lowerValue.Item1} {lowerValue.Item2}) больше или равна верхней ({higherValue.Item1} {higherValue.Item2})."));
            hasErrors = true;
          }
        }

        if (lower.HasValue && !hasErrors)
        {
          var lowerValue = UnitsConvertor.TryConvertBack(lower.Value, unit);
          if (lower.Value < minResistance)
          {
            var minValue = UnitsConvertor.TryConvertBack(minResistance, "Ом");
            LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) нижняя граница сопротивления меньше минимально измеряемого ({minValue.Item1} {minValue.Item2})..");
            model.Errors.Add(IeErrors.CapacityLimitsConflict(numberLine, $"{commandNumber} {mnemonic}",
              $"Нижняя граница сопротивления ({lowerValue.Item1} {lowerValue.Item2}) меньше минимально измеряемого ({minValue.Item1} {minValue.Item2})."));
            hasErrors = true;
          }
          if (lower.Value > maxResistance)
          {
            var maxValue = UnitsConvertor.TryConvertBack(maxResistance, "Ом");

            LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) нижняя граница сопротивления больше максимально возможной ({maxResistance}).");
            model.Errors.Add(IeErrors.CapacityLimitsConflict(numberLine, $"{commandNumber} {mnemonic}",
              $"Нижняя граница сопротивления ({lowerValue.Item1} {lowerValue.Item2}) больше максимально возможной ({maxValue.Item1} {maxValue.Item2})."));
            hasErrors = true;
          }
        }

        if (higher.HasValue && !hasErrors)
        {
          var higherValue = UnitsConvertor.TryConvertBack(higher.Value, unit);

          if (higher.Value > maxResistance)
          {
            var maxValue = UnitsConvertor.TryConvertBack(maxResistance, "Ом");
            LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) верхняя граница сопротивления больше максимально возможной ({maxResistance}).");
            model.Errors.Add(IeErrors.CapacityLimitsConflict(numberLine, $"{commandNumber} {mnemonic}",
              $"Верхняя граница сопротивления ({higherValue.Item1} {higherValue.Item2}) больше максимально возможной ({maxValue.Item1} {maxValue.Item2})."));
            hasErrors = true;
          }
          if (higher.Value < minResistance)
          {
            var minValue = UnitsConvertor.TryConvertBack(minResistance, "Ом");
            LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) верхняя граница сопротивления меньше минимально измеряемого ({minResistance}).");
            model.Errors.Add(IeErrors.CapacityLimitsConflict(numberLine, $"{commandNumber} {mnemonic}",
              $"Верхняя граница сопротивления ({higherValue.Item1} {higherValue.Item2}) меньше минимально измеряемого ({minValue.Item1} {minValue.Item2})."));
            hasErrors = true;
          }
        }

        if (hasErrors == false)
        {
          model.ResistanceUnit = unit ?? string.Empty;

          double finalLower = -1;
          if (lower == null)
          {
            finalLower = minResistance;
          }
          else
          {
            finalLower = lower.Value;
          }
          model.LowerLimitResistance = finalLower;
          model.LowerLimitResistanceSource = $"{finalLower} {unit}";

          double finalHigher = -1;
          if (higher == null)
          {
            finalHigher = maxResistance;
            model.Warnings.Add(GeneralWarnings.DefaultResistainceLowLimit(model.StartLineNumber, $"{commandNumber} {mnemonic}", $"{finalHigher} {model.ResistanceUnit}"));
          }
          else
          {
            finalHigher = (double)higher;
          }
          model.HigherLimitResistance = finalHigher;
          model.HigherLimitResistanceSource = $"{finalHigher} {unit}";

        }

        if (HasInvalidParameterOrder(body, model.AlgorithmKey, lowerLimitResistance ?? higherLimitResistance, time, out string err))
        {
          model.Errors.Add(GeneralErrors.InvalidParameterOrder(mnemonic, numberLine, $"{commandNumber} {mnemonic}", err));
          LogWarning($"Ошибка порядка параметров (строка {numberLine}): {err}");
          return model;
        }

        string bodyNoWs = string.Concat(processedLines.Select(l => Regex.Replace(l ?? string.Empty, @"\s+", "")));

        int firstStar = bodyNoWs.IndexOf('*');
        int lastStar = bodyNoWs.LastIndexOf('*');

        if (firstStar >= 0 && lastStar > firstStar)
        {
          string pointsBlob = bodyNoWs.Substring(firstStar, lastStar - firstStar + 1);
          model.PointsSourse = pointsBlob;
          LogDebug($"Парсинг точек из общего блока: '{pointsBlob}'");

          var (scheme, pointErrors) = PointParser.ParsePoints(pointsBlob, model, rmCommandModel);

          if (pointErrors?.Count > 0)
          {
            foreach (var error in pointErrors)
            {
              error.SourceLineNumber = numberLine;
              error.Command = $"{commandNumber} {mnemonic}";
              model.Errors.Add(error);
              LogError(
                 $"При парсинге точек команды {commandNumber} {mnemonic} произошла ошибка: {error.Description} (строка {error.SourceLineNumber}).");
            }
          }

          if (scheme == null || scheme.IsEmpty())
          {
            LogWarning($"Не найдено ни одной точки (строка {numberLine}): {commandNumber} {mnemonic}");
            model.Errors.Add(IeErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
          }
          else
          {
            model.Scheme = scheme;
            LogInformation(
               $"Схема распознана: цепей={scheme.GroupModels?.Count ?? 0}, частей={scheme.CountParts()}, точек={scheme.CountPoints()}");
          }

          int idxStarInFirstLine = remainder.IndexOf('*');
          int idxStarInSecondLine = remainder.LastIndexOf('*');
          if (idxStarInFirstLine >= 0 && idxStarInSecondLine > idxStarInFirstLine)
          {
            remainder =
                remainder[..idxStarInFirstLine].Trim()
                + remainder[(idxStarInSecondLine + 1)..].Trim();
          }
          else
          {
            remainder = remainder.Trim();
          }
        }
        else
        {
          LogWarning($"Во всём теле команды не найден блок точек '*...*' (строка {numberLine}): {commandNumber} {mnemonic}");
          model.Errors.Add(IeErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
        }

        if (!string.IsNullOrEmpty(remainder))
        {
          model.UnparsedParameters = "! Не распознанные параметры: ";
          model.UnparsedParameters += remainder;
          model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{commandNumber} {mnemonic}"));
        }

        // Валидация
        if (string.IsNullOrWhiteSpace(lowerLimitResistance) && string.IsNullOrWhiteSpace(higherLimitResistance) && string.IsNullOrWhiteSpace(time))
        {
          LogError($"Не удалось распознать параметры в строке: '{remainder}' (строка {numberLine})");
          model.Errors.Add(KsErrors.CannotParseParameters(remainder, numberLine, $"{commandNumber} {mnemonic}"));
        }

        AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

        LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

        return model;
      }
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
      if (idxKey != -1 && idxTime != -1 && idxKey > idxTime)
      {
        errorDescription = "Ключ алгоритма указан после времени.";
        return true;
      }

      if (idxKey != -1 && idxResistance != -1 && idxKey > idxResistance)
      {
        errorDescription = "Ключ алгоритма указан после сопротивления.";
        return true;
      }

      if (idxTime != -1 && idxResistance != -1 && idxResistance > idxTime)
      {
        errorDescription = "Время указано до сопротивления.";
        return true;
      }

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
