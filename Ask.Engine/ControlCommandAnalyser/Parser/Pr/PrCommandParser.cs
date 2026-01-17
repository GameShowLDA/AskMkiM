using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Pr
{
  internal class PrCommandParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic) => mnemonic.Mnemonic.MatchesEnum(MeasurementTypeCommand.PR);

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {

      LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");

      var model = new PrCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      var rmCommandModel = CommandsModel.GetRMModel();

      if (rmCommandModel == null)
      {
        LogError($"Команда РМ не найдена");
        model.Errors.Add(PrErrors.EmptyPoints(model.StartLineNumber, $"{model.CommandNumber}   {model.Mnemonic}"));
      }

      if (lines == null || lines.Count == 0)
      {
        LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
        model.Errors.Add(PrErrors.EmptyCommandBody(model.StartLineNumber, $"{model.CommandNumber}   {model.Mnemonic}"));
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

      string? lowerLimitResistance = null, higherLimitResistance = null, unit = null, time = string.Empty, unitTime = string.Empty;

      remainder = KeyParser.ParseKeys(numberLine, model, remainder);

      remainder = ParsePrParams(remainder, out lowerLimitResistance, out higherLimitResistance, out unit, out time, out unitTime);

      var meter = new DataBaseConfiguration.Services.Device.FastMeterServices().GetAll().FirstOrDefault();
      //var minResistance = Measurement.MeasurementTypeCommand.PR.GetDisplayInfo().LowerLimit;
      if (meter == null)
      {
        LogError($"Не найден быстрый измеритель.");
        model.Errors.Add(GeneralErrors.FastMeterNotFound(numberLine, $"{commandNumber} {mnemonic}"));
        return model;
      }
      else
      {
        // значения по умолчанию
        var commandInfo = EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.PR);

        double defaultLower = commandInfo.LowerLimit; //1Ом
        double defaultHigher = commandInfo.UpperLimit;

        // --- 1️⃣ Парсим входные значения, если они заданы ---
        double? lower = !string.IsNullOrWhiteSpace(lowerLimitResistance)
            ? CommonParameterParser.ParseToDouble(lowerLimitResistance)
            : null;

        double? higher = !string.IsNullOrWhiteSpace(higherLimitResistance)
            ? CommonParameterParser.ParseToDouble(higherLimitResistance)
            : null;

        // --- 2️⃣ Проверка валидности, если обе границы заданы ---

        ProcessResistanceLimits(model, unit, lower, higher);

        // Если есть ошибки правил – нечего проверять оборудование
        bool hasResistanceErrors = model.Errors.Any(e =>
              e.Code == ErrorCode.Pr_ResistanceLimitsConflict ||
              e.Code == ErrorCode.Pr_ResistanceMaxLimitsConflict ||
              e.Code == ErrorCode.Pr_EmptyResistance);


        // Проверка оборудования
        if (hasResistanceErrors == false)
        {
          ValidateAgainstMeter(model, meter);
        }
      }

      double? timeValue = -1;
      if (!string.IsNullOrEmpty(time) && time != null)
      {
        timeValue = CommonParameterParser.ParseToDouble(time);
      }
      else if (!string.IsNullOrEmpty(unitTime))
      {
        timeValue = 1;
        model.Warnings.Add(GeneralWarnings.DefaultTime(model.StartLineNumber, $"{commandNumber} {mnemonic}", $"{timeValue} {unitTime}"));
      }

      if (timeValue.HasValue && timeValue > -1)
      {
        model.Time = timeValue.Value;
      }
      model.TimeSource = time + unitTime;

      string bodyNoWs = string.Concat(processedLines.Select(l => Regex.Replace(l ?? string.Empty, @"\s+", "")));

      // Ищем первую и последнюю '*'
      int firstStar = bodyNoWs.IndexOf('*');
      int lastStar = bodyNoWs.LastIndexOf('*');

      if (firstStar >= 0 && lastStar > firstStar)
      {
        // Выделяем блок точек (включительно) — PointParser сам Trim('*')
        string pointsBlob = bodyNoWs.Substring(firstStar, lastStar - firstStar + 1);
        model.PointsSourse = pointsBlob;
        LogDebug($"Парсинг точек из общего блока: '{pointsBlob}'");

        var (scheme, pointErrors) = PointParser.ParsePoints(pointsBlob, model, rmCommandModel);
        if (model.AlgorithmKey.Contains(AlgorithmKey.ЗР.ToString())
          && pointErrors.FirstOrDefault(item => item.Code == ErrorCode.Gen_InvalidNumberOfDisconnectedRanges) != null)
        {
          pointErrors.Remove(pointErrors.FirstOrDefault(item => item.Code == ErrorCode.Gen_InvalidNumberOfDisconnectedRanges));
        }

        // Поднимем ошибки парсера точек
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

        // Проверим, что схема непуста (есть хотя бы одна точка)
        ValidateScheme(commandNumber, mnemonic, numberLine, model, scheme);

        // Обновим remainder: оставим в нём только то, что до первой '*' в ПЕРВОЙ строке
        int idxStarInFirstLine = remainder.IndexOf('*');
        remainder = idxStarInFirstLine >= 0 ? remainder[..idxStarInFirstLine].Trim() : remainder.Trim();
        if (model.AlgorithmKey.Contains(AlgorithmKey.П.ToString()))
        {
          // находим цепи точек из предыдущей команды проверки
          var newScheme = CommandsModel.CheckKeyP(model, model.Scheme);
          if (newScheme != null)
          {
            model.Scheme = newScheme;
          }
          else
          {
            model.Errors.Add(PrErrors.PreviousCommandHasNoPoints(numberLine, $"{commandNumber} {mnemonic}"));
          }
        }
        if (model.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
        {
          model.Scheme = CommandsModel.CheckKeyS(model.Scheme);
        }
      }
      else if (model.AlgorithmKey.Contains(AlgorithmKey.П.ToString()))
      {
        // находим цепи точек из предыдущей команды проверки
        var newScheme = CommandsModel.CheckKeyP(model, model.Scheme);
        if (newScheme != null)
        {
          model.Scheme = newScheme;
        }
        else
        {
          model.Errors.Add(PrErrors.PreviousCommandHasNoPoints(numberLine, $"{commandNumber} {mnemonic}"));
        }

        if (model.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
        {
          model.Scheme = CommandsModel.CheckKeyS(model.Scheme);
        }
      }
      else if (model.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
      {
        model.Scheme = CommandsModel.CheckKeyS(model.Scheme);
      }
      else
      {
        // Во всём теле команды не нашли пары '*...*' → считаем, что точек нет
        LogWarning($"Во всём теле команды не найден блок точек '*...*' (строка {numberLine}): {commandNumber} {mnemonic}");
        model.Errors.Add(PrErrors.EmptyPoints(model.StartLineNumber, $"{model.CommandNumber}   {model.Mnemonic}"));
      }

      if (!string.IsNullOrEmpty(remainder))
      {
        model.UnparsedParameters = "! Не распознанные параметры: ";
        model.UnparsedParameters += remainder;
        model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{commandNumber} {mnemonic}"));
      }

      // Валидация
      if (string.IsNullOrWhiteSpace(model.DisconnectedLowerLimitResistanceSource) && string.IsNullOrWhiteSpace(model.DisconnectedHigherLimitResistanceSource)
        && string.IsNullOrWhiteSpace(model.ConnectedLowerLimitResistanceSource) && string.IsNullOrWhiteSpace(model.ConnectedHigherLimitResistanceSource))
      {
        LogError($"Не удалось распознать параметры в строке: '{remainder}' (строка {numberLine})");
        model.Errors.Add(PrErrors.CannotParseParameters(
          $"Сопротивление было неправильно задано, или неверно указаны его границы", 
          model.StartLineNumber, 
          $"{model.CommandNumber}   {model.Mnemonic}"));
      }

      // Валидация
      if (string.IsNullOrWhiteSpace(model.ConnectedHigherLimitResistanceSource)&&!model.AlgorithmKey.Contains(AlgorithmKey.ЗС.ToString()))
      {
        LogError($"Не удалось распознать параметры в строке: '{remainder}' (строка {numberLine})");
        model.Errors.Add(PrErrors.ResistanceLimitsConflict(
          model.StartLineNumber, 
          $"{model.CommandNumber}   {model.Mnemonic}",
          $"Не указана верхняя граница при проверке на сообщение"));
      }

      AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

      LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }

    private static void ValidateScheme(string commandNumber, string mnemonic, int numberLine, PrCommandModel model, SchemeModel? scheme)
    {
      if (scheme == null || scheme.IsEmpty())
      {
        LogWarning($"Не найдено ни одной точки (строка {numberLine}): {commandNumber} {mnemonic}");
        model.Errors.Add(PrErrors.EmptyPoints(model.StartLineNumber, $"{model.CommandNumber}   {model.Mnemonic}"));
      }
      else
      {
        model.Scheme = scheme; // ← просто присваиваем схему в модель
        LogInformation(
           $"Схема распознана: цепей={scheme.GroupModels?.Count ?? 0}, частей={scheme.CountParts()}, точек={scheme.CountPoints()}");
      }
    }

    private static string ParsePrParams(string remainder, out string? lowerLimitResistance, out string? higherLimitResistance, out string? unit, out string? time, out string? unitTime)
    {
      (lowerLimitResistance, higherLimitResistance, unit, remainder) = CommonParameterParser.ResistanceParser.ParseResistanceRangeWithR(remainder);
      LogDebug($"После парсинга напряжения: нижняя граница сопртивления='{lowerLimitResistance}',верхняя граница сопртивления='{higherLimitResistance}', единица измерения = '{unit}' remainder='{remainder}'");

      (time, unitTime, remainder) = CommonParameterParser.TimeParser.ParseTime(remainder);
      LogDebug($"После парсинга времени: time='{time}{unitTime}', remainder='{remainder}'");
      return remainder;
    }

    private static ResistanceRangeAttribute? GetResistanceRangeAttr(PrCommandModel model)
    {
      return (ResistanceRangeAttribute?)Attribute.GetCustomAttribute(
          model.GetType(),
          typeof(ResistanceRangeAttribute));
    }

    private void ProcessResistanceLimits(PrCommandModel model, string? unit, double? lower, double? higher)
    {
      var rangeAttr = GetResistanceRangeAttr(model);
      if (rangeAttr == null)
      {
        LogError("Для PrCommandModel не найден атрибут ResistanceRange.");
        model.Errors.Add(PrErrors.CannotParseParameters("Ошибка конфигурации обработчика ПР", model.StartLineNumber, $"{model.CommandNumber} {model.Mnemonic}"));
        return;
      }

      double PR_MIN = rangeAttr.Min;
      double PR_MAX = rangeAttr.Max;
      double PR_DEFAULT_LOWER = rangeAttr.DefaultLower;
      char infinity = '\u221E';

      // Конвертация
      (double? valLower, string lowerUnit) = lower.HasValue ? UnitsConvertor.TryConvertBack(lower.Value, unit) : (null, unit);

      (double? valHigher, string higherUnit) = higher.HasValue ? UnitsConvertor.TryConvertBack(higher.Value, unit) : (null, unit);

      // 1) ЕСЛИ ОБЕ ГРАНИЦЫ ЗАДАНЫ
      if (valLower.HasValue && valHigher.HasValue)
      {
        // 1) Проверка: нижняя > верхней (в ОМАХ)
        if (lower!.Value > higher!.Value)
        {
          model.Errors.Add(
            PrErrors.ResistanceLimitsConflict(
              model.StartLineNumber,
              $"{model.CommandNumber} {model.Mnemonic}",
              $"Нижняя граница ({valLower} {lowerUnit}) больше верхней ({valHigher} {higherUnit})"));
          return;
        }

        // 2) Нижняя < минимально допустимой (в ОМАХ)
        if (lower.Value < PR_MIN)
        {
          model.Errors.Add(
            PrErrors.ResistanceLimitsConflict(
              model.StartLineNumber,
              $"{model.CommandNumber} {model.Mnemonic}",
              $"Нижняя граница ({valLower} {lowerUnit}) меньше минимально допустимой ({PR_MIN} Ом)"));
          return;
        }

        // 3) Верхняя > максимально допустимой (в ОМАХ)
        if (higher.Value > PR_MAX)
        {
          model.Errors.Add(
            PrErrors.ResistanceLimitsConflict(
              model.StartLineNumber,
              $"{model.CommandNumber} {model.Mnemonic}",
              $"Верхняя граница ({valHigher} {higherUnit}) больше максимально допустимой ({PR_MAX} Ом)"));
          return;
        }

        // ОК, сохраняем как есть
        // проверка на разобщение
        model.DisconnectedLowerLimitResistance = lower.Value;
        model.DisconnectedLowerLimitResistanceSource = $"{valLower} {lowerUnit}";

        model.DisconnectedHigherLimitResistance = higher.Value;
        model.DisconnectedHigherLimitResistanceSource = $"{valHigher} {higherUnit}";

        //проверка на сообщение
        model.ConnectedLowerLimitResistance = lower.Value;
        model.ConnectedLowerLimitResistanceSource = $"{valLower} {lowerUnit}";

        model.ConnectedHigherLimitResistance = higher.Value;
        model.ConnectedHigherLimitResistanceSource = $"{valHigher} {higherUnit}";

        return;
      }

      // 2) ЕСЛИ ЗАДАНА ТОЛЬКО ВЕРХНЯЯ
      if (!valLower.HasValue && valHigher.HasValue)
      {
        double lowOhm = 0.0; // по правилам

        if (higher!.Value > PR_MAX)
        {
          model.Errors.Add(
            PrErrors.ResistanceLimitsConflict(
              model.StartLineNumber,
              $"{model.CommandNumber} {model.Mnemonic}",
              $"Верхняя граница ({valHigher} {higherUnit}) больше максимально допустимой ({PR_MAX} Ом)"));
          return;
        }

        // проверка на разобщение
        model.DisconnectedLowerLimitResistance = higher.Value;
        model.DisconnectedLowerLimitResistanceSource = $"{valHigher} {higherUnit}";

        model.DisconnectedHigherLimitResistance = null;
        model.DisconnectedHigherLimitResistanceSource = $"{infinity} Ом";

        //проверка на сообщение
        model.ConnectedLowerLimitResistance = lowOhm;
        model.ConnectedLowerLimitResistanceSource = $"{lowOhm} Ом";

        model.ConnectedHigherLimitResistance = higher.Value;
        model.ConnectedHigherLimitResistanceSource = $"{valHigher} {higherUnit}";

        return;
      }

      // 3) ЕСЛИ ЗАДАНА ТОЛЬКО НИЖНЯЯ
      if (valLower.HasValue && !valHigher.HasValue)
      {
        if (lower!.Value < PR_MIN)
        {
          model.Errors.Add(
            PrErrors.ResistanceLimitsConflict(
              model.StartLineNumber,
              $"{model.CommandNumber} {model.Mnemonic}",
              $"Нижняя граница ({valLower} {lowerUnit}) меньше минимально допустимой ({PR_MIN} Ом)"));
          return;
        }

        // проверка на разобщение
        model.DisconnectedLowerLimitResistance = higher.Value;
        model.DisconnectedLowerLimitResistanceSource = $"{valHigher} {higherUnit}";

        model.DisconnectedHigherLimitResistance = null;
        model.DisconnectedHigherLimitResistanceSource = $"{infinity} Ом";

        //проверка на сообщение
        model.ConnectedLowerLimitResistance = lower.Value; 
        model.ConnectedLowerLimitResistanceSource = $"{valLower} {lowerUnit}";

        model.ConnectedHigherLimitResistance = null;
        model.ConnectedHigherLimitResistanceSource = null;

        return;
      }

      // 4) НИ ОДНОЙ ГРАНИЦЫ НЕТ
      // проверка на разобщение
      model.DisconnectedLowerLimitResistance = PR_DEFAULT_LOWER;
      model.DisconnectedLowerLimitResistanceSource = $"{PR_DEFAULT_LOWER} Ом"; 

      model.DisconnectedHigherLimitResistance = null;
      model.DisconnectedHigherLimitResistanceSource = $"{infinity} Ом";

      //проверка на сообщение
      model.ConnectedLowerLimitResistance = PR_DEFAULT_LOWER;
      model.ConnectedLowerLimitResistanceSource = $"{PR_DEFAULT_LOWER} Ом"; 

      model.ConnectedHigherLimitResistance = null;
      model.ConnectedHigherLimitResistanceSource = null;
    }

    private void ValidateAgainstMeter(PrCommandModel model, IFastMeter meter)
    {
      var connectedLower = model.ConnectedLowerLimitResistance;
      var connectedHigher = model.ConnectedHigherLimitResistance;
      var disconnectedLower = model.DisconnectedLowerLimitResistance;
      var disconnectedHigher = model.DisconnectedHigherLimitResistance;
      // Проверка нижней границы
      if (connectedLower.HasValue && connectedLower.Value < 0)
      {
        model.Errors.Add(
            PrErrors.EquipmentOutOfRange(
                model.StartLineNumber,
                $"{model.CommandNumber} {model.Mnemonic}",
                $"Нижняя граница {connectedLower} Ом проверки на сообщение ниже минимально измеряемой прибором (0 Ом)"));
      }
      if (disconnectedLower.HasValue && disconnectedLower.Value < 0)
      {
        model.Errors.Add(
            PrErrors.EquipmentOutOfRange(
                model.StartLineNumber,
                $"{model.CommandNumber} {model.Mnemonic}",
                $"Нижняя граница {disconnectedLower} Ом проверки на разобщение ниже минимально измеряемой прибором (0 Ом)"));
      }

      if (connectedLower.HasValue && connectedLower.Value > meter.MaxContinuityResistance)
      {
        model.Errors.Add(
            PrErrors.EquipmentOutOfRange(
                model.StartLineNumber,
                $"{model.CommandNumber} {model.Mnemonic}",
                $"Нижняя граница {connectedLower} Ом проверки на сообщение выше максимально измеряемой прибором ({meter.MaxContinuityResistance} Ом)"));
      }

      if (disconnectedLower.HasValue && disconnectedLower.Value > meter.MaxContinuityResistance)
      {
        model.Errors.Add(
            PrErrors.EquipmentOutOfRange(
                model.StartLineNumber,
                $"{model.CommandNumber} {model.Mnemonic}",
                $"Нижняя граница {disconnectedLower} Ом проверки на разобщение выше максимально измеряемой прибором ({meter.MaxContinuityResistance} Ом)"));
      }

      // Проверка верхней границы
      if (connectedHigher.HasValue && connectedHigher.Value < 0)
      {
        model.Errors.Add(
            PrErrors.EquipmentOutOfRange(
                model.StartLineNumber,
                $"{model.CommandNumber} {model.Mnemonic}",
                $"Верхняя граница {connectedHigher} Ом проверки на сообщение ниже минимально измеряемой прибором (0 Ом)"));
      }

      if (disconnectedHigher.HasValue && disconnectedHigher.Value < 0)
      {
        model.Errors.Add(
            PrErrors.EquipmentOutOfRange(
                model.StartLineNumber,
                $"{model.CommandNumber} {model.Mnemonic}",
                $"Верхняя граница {disconnectedHigher} Ом проверки на разобщение ниже минимально измеряемой прибором (0 Ом)"));
      }

      if (connectedHigher.HasValue && connectedHigher.Value > meter.MaxContinuityResistance)
      {
        model.Errors.Add(
            PrErrors.EquipmentOutOfRange(
                model.StartLineNumber,
                $"{model.CommandNumber} {model.Mnemonic}",
                $"Верхняя граница {connectedHigher} Ом проверки на сообщение выше максимально измеряемой прибором ({meter.MaxContinuityResistance} Ом)"));
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
