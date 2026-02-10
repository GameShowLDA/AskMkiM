using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;
using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Ie
{
  /// <summary>
  /// Парсер для команд ИЕ (измерение емкости).
  /// </summary>
  [AllowedKeys(Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Д)]
  internal class IeCommandParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic)
    => mnemonic.Mnemonic.MatchesEnum(MeasurementTypeCommand.IE);

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");
      var model = new IeCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      var rmCommandModel = CommandsModel.GetRMModel();

      if (rmCommandModel == null)
      {
        LogError($"Команда РМ не найдена");
        model.Errors.Add(IeErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
      }

      if (lines == null || lines.Count == 0)
      {
        LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
        model.Errors.Add(IeErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
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

      string? lowerLimitCapacity = null, higherLimitCapacity = null, unit = null;

      remainder = KeyParser.ParseKeys(numberLine, model, remainder);

      (lowerLimitCapacity, higherLimitCapacity, unit, remainder) = CommonParameterParser.CapacityParser.ParseCapacityRange(remainder);
      LogDebug($"После парсинга электрической ёмкости: нижняя='{lowerLimitCapacity}', верхняя='{higherLimitCapacity}', единица='{unit}', remainder='{remainder}'");

      if (string.IsNullOrEmpty(lowerLimitCapacity))
      {
        model.Errors.Add(IeErrors.EmptyLowerCapacity(numberLine, $"{commandNumber} {mnemonic}"));
        LogWarning($"Не указана нижняя граница электрической емкости (строка {numberLine}): {commandNumber} {mnemonic}");

        if (!string.IsNullOrEmpty(remainder))
        {
          model.UnparsedParameters = "! Не распознанные параметры: " + remainder;
          model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{commandNumber} {mnemonic}"));
        }

        return model;
      }

      double? lower = CommonParameterParser.ParseToDouble(lowerLimitCapacity);
      double? higher = !string.IsNullOrWhiteSpace(higherLimitCapacity) ? CommonParameterParser.ParseToDouble(higherLimitCapacity) : null;

      var meter = new DataBaseConfiguration.Services.Device.FastMeterServices().GetAll().FirstOrDefault();
      if (meter == null)
      {
        LogError($"Не найден быстрый измеритель.");
        model.Errors.Add(GeneralErrors.FastMeterNotFound(numberLine, $"{commandNumber} {mnemonic}"));
        return model;
      }
      else
      {
        var commandInfo = EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.IE);

        double minCapacity = commandInfo.LowerLimit;
        double maxCapacity = commandInfo.UpperLimit;
        string defaultCapacityUnit = commandInfo.Unit;

        var lowerLimit = UnitsConvertor.TryParseValue($"{minCapacity}", commandInfo.Unit);
        var higherLimit = UnitsConvertor.TryParseValue($"{maxCapacity}", commandInfo.Unit);

        bool hasErrors = false;

        if (lower.HasValue && higher.HasValue)
        {
          if (lower.Value >= higher.Value)
          {
            var lowerValue = UnitsConvertor.TryConvertBack(lower.Value, unit);
            var higherValue = UnitsConvertor.TryConvertBack(higher.Value, unit);
            LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) нижняя граница электрической емкости больше или равна верхней.");
            model.Errors.Add(IeErrors.CapacityLimitsConflict(numberLine, $"{commandNumber} {mnemonic}",
              $"Нижняя граница электрической емкости ({lowerValue.Item1} {lowerValue.Item2}) " +
              $"больше или равна верхней ({higherValue.Item1} {higherValue.Item2})."));
            hasErrors = true;
          }
        }

        if (lower.HasValue && !hasErrors)
        {
          var lowerValue = UnitsConvertor.TryParseValue($"{lower.Value}", unit);
          if (lowerValue < lowerLimit)
          {
            LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) " +
               $"нижняя граница электрической емкости меньше минимально измеряемой ({lowerLimit} {defaultCapacityUnit}).");
            model.Errors.Add(IeErrors.CapacityLimitsConflict(numberLine, $"{commandNumber} {mnemonic}",
              $"Нижняя граница электрической емкости ({lowerValue} {unit}) " +
              $"меньше минимально измеряемой ({lowerLimit} {defaultCapacityUnit})."));
            hasErrors = true;
          }
          if (lowerValue > higherLimit)
          {
            LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) верхняя граница электрической емкости больше максимально возможной ({maxCapacity}).");
            model.Errors.Add(IeErrors.CapacityLimitsConflict(numberLine, $"{commandNumber} {mnemonic}",
              $"Нижняя граница электрической емкости ({lowerValue} {unit}) больше максимально возможной ({higherLimit} {defaultCapacityUnit})."));
            hasErrors = true;
          }
        }

        if (higher.HasValue && !hasErrors)
        {
          var higherValue = UnitsConvertor.TryParseValue($"{higher.Value}", unit);

          if (higherValue > higherLimit)
          {
            LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) " +
               $"верхняя граница электрической емкости больше максимально возможной ({maxCapacity}).");
            model.Errors.Add(IeErrors.CapacityLimitsConflict(numberLine, $"{commandNumber} {mnemonic}",
              $"Верхняя граница электрической емкости ({higherValue} {unit}) " +
              $"больше максимально возможной ({higherLimit} {defaultCapacityUnit})."));
            hasErrors = true;
          }
          if (higherValue < lowerLimit)
          {
            LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) " +
               $"нижняя граница электрической емкости меньше минимально измеряемой ({lowerLimit} {defaultCapacityUnit}).");
            model.Errors.Add(IeErrors.CapacityLimitsConflict(numberLine, $"{commandNumber} {mnemonic}",
              $"Верхняя граница электрической емкости ({higherValue} {unit}) " +
              $"меньше минимально измеряемой ({lowerLimit} {defaultCapacityUnit})."));
            hasErrors = true;
          }
        }

        if (hasErrors == false)
        {
          model.CapacityUnit = unit ?? string.Empty;
          model.LowerLimitCapacity = lower.Value;
          model.LowerLimitCapacitySource = $"{lower.Value} {unit}";

          double finalHigher = -1;
          if (higher == null)
          {
            finalHigher = maxCapacity;
            model.Warnings.Add(GeneralWarnings.DefaultCapacityHighLimit(model.StartLineNumber, $"{commandNumber} {mnemonic}", $"{finalHigher} {model.CapacityUnit}"));
          }
          else
          {
            finalHigher = higher.Value;
          }
          model.HigherLimitCapacity = finalHigher;
          model.HigherLimitCapacitySource = $"{finalHigher} {unit}";
        }

        if (HasInvalidParameterOrder(body, model.AlgorithmKey, lowerLimitCapacity ?? higherLimitCapacity, out string err))
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
        if (string.IsNullOrWhiteSpace(lowerLimitCapacity) && string.IsNullOrWhiteSpace(higherLimitCapacity))
        {
          LogError($"Не удалось распознать параметры в строке: '{remainder}' (строка {numberLine})");
          model.Errors.Add(IeErrors.CannotParseParameters(remainder, numberLine, $"{commandNumber} {mnemonic}"));
        }

        AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

        LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

        return model;
      }
    }

    public static bool HasInvalidParameterOrder(string firstLine, List<string> algorithmKeys, string? resistanceStart, out string errorDescription)
    {
      errorDescription = string.Empty;

      int idxKey = -1;
      int idxResistance = -1;
      int idxPoint = firstLine.IndexOf('*');

      foreach (var key in algorithmKeys)
      {
        int idx = firstLine.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0 && (idxKey == -1 || idx < idxKey))
          idxKey = idx;
      }

      // Сопротивление
      if (!string.IsNullOrWhiteSpace(resistanceStart))
      {
        idxResistance = firstLine.IndexOf(resistanceStart, StringComparison.OrdinalIgnoreCase);
      }

      // Ключ должен идти до сопротивления
      if (idxKey != -1 && idxResistance != -1 && idxKey > idxResistance)
      {
        errorDescription = "Ключ алгоритма указан после электрической емкости.";
        return true;
      }

      // Все параметры должны быть до точек
      if (idxPoint != -1)
      {
        if ((idxKey != -1 && idxKey > idxPoint)
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
