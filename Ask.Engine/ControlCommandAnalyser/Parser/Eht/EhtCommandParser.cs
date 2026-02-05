using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;


namespace Ask.Engine.ControlCommandAnalyser.Parser.Eht
{
  internal class EhtCommandParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic) => mnemonic.Mnemonic.MatchesEnum(MeasurementTypeCommand.EHT);

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");

      var model = new EhtCommandModel()
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      var rmCommandModel = CommandsModel.GetRMModel();

      if (rmCommandModel == null)
      {
        LogError($"Команда РМ не найдена");
        model.Errors.Add(EhtErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
      }

      if (lines == null || lines.Count == 0)
      {
        LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
        model.Errors.Add(EhtErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
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

      // Склеиваем всё в одну строку и удаляем \r \n \t
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

      string? lowerLimitResistance = null, higherLimitResistance = null, unit = null,
        time = string.Empty, unitTime = string.Empty,
        cabelLimitResistance = null, cabelUnit = null;

      remainder = KeyParser.ParseKeys(numberLine, model, remainder);

      (lowerLimitResistance, higherLimitResistance, unit, remainder) = CommonParameterParser.ResistanceParser.ParseResistanceRangeWithR(remainder);
      LogDebug($"После парсинга напряжения: нижняя граница сопртивления='{lowerLimitResistance}',верхняя граница сопртивления='{higherLimitResistance}', единица измерения = '{unit}' remainder='{remainder}'");

      (cabelLimitResistance, cabelUnit, remainder) = CommonParameterParser.ResistanceParser.ParseCabelResistance(remainder);
      LogDebug($"После парсинга сопротивления проводов: сопртивление='{cabelLimitResistance}', единица измерения = '{cabelUnit}' remainder='{remainder}'");

      (time, unitTime, remainder) = CommonParameterParser.TimeParser.ParseTime(remainder);
      LogDebug($"После парсинга времени: time='{time}{unitTime}', remainder='{remainder}'");

      bool hasResistanceErrors = false;

      // значения по умолчанию
      var commandInfo = EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.EHT);

      double defaultLower = commandInfo.LowerLimit;
      double defaultHigher = commandInfo.UpperLimit;
      string defaultUnit = commandInfo.Unit;

      double? lower = !string.IsNullOrWhiteSpace(lowerLimitResistance)
          ? CommonParameterParser.ParseToDouble(lowerLimitResistance)
          : 0;

      double? higher = !string.IsNullOrWhiteSpace(higherLimitResistance)
          ? CommonParameterParser.ParseToDouble(higherLimitResistance)
          : 1;

      double? cabelLimit = !string.IsNullOrWhiteSpace(cabelLimitResistance)
          ? CommonParameterParser.ParseToDouble(cabelLimitResistance)
          : null;
      if (string.IsNullOrEmpty(unit))
      {
        unit = defaultUnit;
      }

      if (lower.HasValue && higher.HasValue)
      {
        var higherValue = UnitsConvertor.TryConvertBack(higher.Value, unit);
        var lowerValue = UnitsConvertor.TryConvertBack(higher.Value, unit);
        var minValue = UnitsConvertor.TryConvertBack(defaultLower, commandInfo.Unit);

        if (lower.Value > higher.Value)
        {
          LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) нижняя граница сопротивления ({lowerValue.Item1} {lowerValue.Item2}) больше верхней.");
          model.Errors.Add(EhtErrors.ResistanceLimitsConflict(numberLine, $"{commandNumber} {mnemonic}", $"Нижняя граница сопротивления ({lowerValue.Item1} {lowerValue.Item2}) больше верхней ({higherValue.Item1} {higherValue.Item2})."));
          hasResistanceErrors = true;
        }
        else if (lower.Value > defaultHigher)
        {
          LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) нижняя граница сопротивления ({lowerValue.Item1} {lowerValue.Item2}) больше максимально допустимой ({defaultHigher} {defaultUnit})");
          model.Errors.Add(EhtErrors.ResistanceMaxLimitsConflict(numberLine, $"{commandNumber} {mnemonic}", defaultHigher, defaultUnit));
          hasResistanceErrors = true;
        }
        else if (lower.Value < defaultLower)
        {
          LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) нижняя граница сопротивления меньше минимально возможной ({defaultLower}).");
          model.Errors.Add(EhtErrors.ResistanceLimitsConflict(numberLine, $"{commandNumber} {mnemonic}", $"Нижняя граница сопротивления ({lowerValue.Item1} {lowerValue.Item2}) меньше минимально возможной ППУ({minValue.Item1} {minValue.Item2})."));
          hasResistanceErrors = true;
        }
        else if (higher.Value < defaultLower)
        {
          LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) верхняя граница сопротивления ({higherValue.Item1} {higherValue.Item2}) меньше минимально возможной ({minValue.Item1} {minValue.Item2}).");
          model.Errors.Add(EhtErrors.ResistanceLimitsConflict(numberLine, $"{commandNumber} {mnemonic}", $"Верхняя граница сопротивления ({higherValue.Item1} {higherValue.Item2}) меньше минимально возможной ППУ ({minValue.Item1} {minValue.Item2})."));
          hasResistanceErrors = true;
        }
        else if (higher.Value > defaultHigher)
        {
          LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) верхняя граница сопротивления ({higherValue.Item1} {higherValue.Item2}) больше максимально допустимой ({defaultHigher} {defaultUnit})");
          model.Errors.Add(EhtErrors.ResistanceMaxLimitsConflict(numberLine, $"{commandNumber} {mnemonic}", defaultHigher, defaultUnit));
          hasResistanceErrors = true;
        }
      }

      if (hasResistanceErrors == false)
      {
        string unitFinal = !string.IsNullOrWhiteSpace(unit) ? unit : defaultUnit;
        double lowerFinal = -1;
        if (lower == null)
        {
          lowerFinal = defaultLower;
        }
        else
        {
          lowerFinal = lower.Value;
        }
        double higherFinal = -1;
        if (higher == null)
        {
          higherFinal = defaultHigher;
          model.Warnings.Add(GeneralWarnings.DefaultResistainceHighLimit(model.StartLineNumber, $"{commandNumber} {mnemonic}", $"{higherFinal} {unitFinal}"));
        }
        else
        {
          higherFinal = higher.Value;
        }
        string cabelUnitFinal = !string.IsNullOrWhiteSpace(cabelUnit) ? cabelUnit : defaultUnit;

        if (!string.IsNullOrWhiteSpace(cabelLimitResistance))
        {
          double cabelFinal = cabelLimit ?? 0;
          if (cabelLimit == null)
          {
            cabelLimit = 0;
            model.Warnings.Add(GeneralWarnings.DefaultResistainceLowLimit(model.StartLineNumber, $"{commandNumber} {mnemonic}", $"{cabelLimit} {cabelUnit}"));
          }
          else
          {
            cabelFinal = cabelLimit.Value;
          }

          model.CabelResistance = cabelFinal;
          model.CabelResistanceSource = $"{cabelFinal} {cabelUnit}";
        }

        model.LowerLimitResistance = lowerFinal;
        model.LowerLimitResistanceSource = $"{lowerFinal} {unitFinal}";

        model.HigherLimitResistance = higherFinal;
        model.HigherLimitResistanceSource = $"{higherFinal} {unitFinal}";
      }

      double? timeValue = -1;
      if (!string.IsNullOrEmpty(time) && time != null)
      {
        timeValue = CommonParameterParser.ParseToDouble(time);
      }
      else if (!string.IsNullOrEmpty(unitTime))
      {
        timeValue = 1;
        model.Warnings.Add(GeneralWarnings.DefaultTime(model.StartLineNumber, $"{commandNumber} {mnemonic}", $"{timeValue}{unitTime}"));
      }

      if (timeValue.HasValue && timeValue > -1)
      {
        model.Time = timeValue.Value;
      }
      model.TimeSource = time + unitTime;

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
          model.Errors.Add(EhtErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
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
        model.Errors.Add(EhtErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
      }

      if (!string.IsNullOrEmpty(remainder))
      {
        model.UnparsedParameters = "! Не распознанные параметры: ";
        model.UnparsedParameters += remainder;
        model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{commandNumber} {mnemonic}"));
      }

      // Валидация
      if (string.IsNullOrWhiteSpace(model.LowerLimitResistanceSource) && string.IsNullOrWhiteSpace(model.HigherLimitResistanceSource))
      {
        LogError($"Не удалось распознать параметры в строке: '{remainder}' (строка {numberLine})");
        model.Errors.Add(EhtErrors.CannotParseParameters($"сопротивление было неправильно задано, или неверно указаны границы сопроитвления", numberLine, $"{commandNumber} {mnemonic}"));
      }

      AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

      LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }
  }
}
