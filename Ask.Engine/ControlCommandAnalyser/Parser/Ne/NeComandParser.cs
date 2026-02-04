using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Ne
{
  public class NeComandParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic) => mnemonic.Mnemonic.MatchesEnum(MeasurementTypeCommand.NE);

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {

      LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");

      var model = new NeCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      var rmCommandModel = CommandsModel.GetRMModel();

      if (rmCommandModel == null)
      {
        LogError($"Команда РМ не найдена");
        model.Errors.Add(NeErrors.EmptyPoints(model.StartLineNumber, $"{model.CommandNumber}   {model.Mnemonic}"));
      }

      if (lines == null || lines.Count == 0)
      {
        LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
        model.Errors.Add(NeErrors.EmptyCommandBody(model.StartLineNumber, $"{model.CommandNumber}   {model.Mnemonic}"));
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

      string? lowerLimitVoltage = null, higherLimitVoltage = null, unitVoltage = null;

      remainder = KeyParser.ParseKeys(numberLine, model, remainder);

      remainder = ParseNeParams(remainder, out lowerLimitVoltage, out higherLimitVoltage, out unitVoltage);

      // значения по умолчанию
      var commandInfo = EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.NE);

      double defaultLower = commandInfo.LowerLimit; //1Ом
      double defaultHigher = commandInfo.UpperLimit;

      // --- 1️⃣ Парсим входные значения, если они заданы ---
      double? lower = !string.IsNullOrWhiteSpace(lowerLimitVoltage)
          ? CommonParameterParser.ParseToDouble(lowerLimitVoltage)
          : null;

      double? higher = !string.IsNullOrWhiteSpace(higherLimitVoltage)
          ? CommonParameterParser.ParseToDouble(higherLimitVoltage)
          : null;

      // --- 2️⃣ Проверка валидности, если обе границы заданы ---

      ProcessVoltageLimits(model, unitVoltage, lower, higher, defaultHigher, defaultLower);


      if (!model.AlgorithmKey.Contains(AlgorithmKey.Н.ToString()))
      {
        model.Voltage = defaultHigher;
        model.VoltageSource = $"{defaultHigher}{commandInfo.Unit}";
        model.VoltageUnit = commandInfo.Unit;
      }

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

        var parts = PointParser.ExtractSigns(pointsBlob);
        var scheme = new SchemeModel(new List<GroupModel>());
        var finalGroups = new List<GroupModel>();
        var allErrors = new List<ErrorItem>();

        foreach (var part in parts)
        {
          var (parsedScheme, pointErrors) = PointParser.ParsePoints(
              part.CleanExpr,
              model,
              rmCommandModel
          );

          if (pointErrors?.Count > 0)
            allErrors.AddRange(pointErrors);

          if (parsedScheme?.GroupModels == null)
            continue;

          foreach (var group in parsedScheme.GroupModels)
          {
            foreach (var chain in group.ChainModels)
            {
              // применяем знак к цепи
              ApplySign(chain, part.Sign, model);

              // добавляем цепь в итоговую схему
              finalGroups.Add(
                  new GroupModel(new List<ChainModel> { chain })
              );
            }
          }
        }

        scheme = new SchemeModel(finalGroups);

        // Поднимем ошибки парсера точек
        if (allErrors?.Count > 0)
        {
          foreach (var error in allErrors)
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
        // Во всём теле команды не нашли пары '*...*' → считаем, что точек нет
        LogWarning($"Во всём теле команды не найден блок точек '*...*' (строка {numberLine}): {commandNumber} {mnemonic}");
        model.Errors.Add(NeErrors.EmptyPoints(model.StartLineNumber, $"{model.CommandNumber}   {model.Mnemonic}"));
      }

      if (!string.IsNullOrEmpty(remainder))
      {
        model.UnparsedParameters = "! Не распознанные параметры: ";
        model.UnparsedParameters += remainder;
        model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{commandNumber} {mnemonic}"));
      }

      // Валидация
      if (string.IsNullOrWhiteSpace(model.HigherLimitVoltageSource) && string.IsNullOrWhiteSpace(model.LowerLimitVoltageSource))
      {
        LogError($"Не удалось распознать параметры в строке: '{remainder}' (строка {numberLine})");
        model.Errors.Add(NeErrors.CannotParseParameters(
          $"Диапазон напряжения был неправильно задан или неверно указаны его границы",
          model.StartLineNumber,
          $"{model.CommandNumber}   {model.Mnemonic}"));
      }

      AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

      LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }

    private static void ValidateScheme(string commandNumber, string mnemonic, int numberLine, NeCommandModel model, SchemeModel? scheme)
    {
      if (scheme == null || scheme.IsEmpty())
      {
        LogWarning($"Не найдено ни одной точки (строка {numberLine}): {commandNumber} {mnemonic}");
        model.Errors.Add(NeErrors.EmptyPoints(model.StartLineNumber, $"{model.CommandNumber}   {model.Mnemonic}"));
      }
      else
      {
        model.Scheme = scheme; // ← просто присваиваем схему в модель
        LogInformation(
           $"Схема распознана: цепей={scheme.GroupModels?.Count ?? 0}, частей={scheme.CountParts()}, точек={scheme.CountPoints()}");
      }
    }

    private static string ParseNeParams(string remainder, out string? lowerLimitVoltage, out string? higherLimitVoltage, out string? unit)
    {
      // парсинг диапазона напряжения, силы тока, напряжения. Силу тока игнорим, напряжение задаем по умолчанию 10В.
      (lowerLimitVoltage, higherLimitVoltage, unit, remainder) = CommonParameterParser.VoltageParser.ParseVoltageRange(remainder);
      LogDebug($"После парсинга напряжения: нижняя граница напряжения='{lowerLimitVoltage}',верхняя граница напряжения='{higherLimitVoltage}', единица измерения = '{unit}' remainder='{remainder}'");

      (var amperage, var unitAmperage, remainder) = CommonParameterParser.AmperageParser.ParseAmperage(remainder);
      LogDebug($"После парсинга силы тока: сила тока='{amperage}', единица измерения = '{unitAmperage}', remainder='{remainder}'");

      (var voltage, var unitVoltage, remainder) = CommonParameterParser.VoltageParser.ParseVoltage(remainder);
      LogDebug($"После парсинга напряжения: напряжение='{voltage}',единица измерения = '{unitVoltage}' remainder='{remainder}'");

      return remainder;
    }

    private void ProcessVoltageLimits(NeCommandModel model, string? unit, double? lower, double? higher, double defaultHigher, double defaultLower)
    {
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
            NeErrors.VoltageLimitsConflict(
              model.StartLineNumber,
              $"{model.CommandNumber} {model.Mnemonic}",
              $"Нижняя граница ({valLower} {lowerUnit}) больше верхней ({valHigher} {higherUnit})"));
          return;
        }

        // 2) Нижняя < минимально допустимой (в ОМАХ)
        if (lower.Value < defaultLower)
        {
          model.Errors.Add(
            NeErrors.VoltageLimitsConflict(
              model.StartLineNumber,
              $"{model.CommandNumber} {model.Mnemonic}",
              $"Нижняя граница ({valLower} {lowerUnit}) меньше минимально допустимой ({defaultLower} В)"));
          return;
        }

        // 3) Верхняя > максимально допустимой (в ОМАХ)
        if (higher.Value > defaultHigher)
        {
          model.Errors.Add(
            NeErrors.VoltageLimitsConflict(
              model.StartLineNumber,
              $"{model.CommandNumber} {model.Mnemonic}",
              $"Верхняя граница ({valHigher} {higherUnit}) больше максимально допустимой ({defaultHigher} В)"));
          return;
        }

        // ОК, сохраняем как есть
        model.HigherLimitVoltage = higher.Value;
        model.HigherLimitVoltageSource = $"{valHigher}{higherUnit}";

        model.LowerLimitVoltage = lower.Value;
        model.LowerLimitVoltageSource = $"{valLower}{lowerUnit}";

        return;
      }
      else
      {
        model.Errors.Add(
            PrErrors.EmptyResistance(
              model.StartLineNumber,
              $"{model.CommandNumber} {model.Mnemonic}"));
        return;
      }
    }

    void ApplySign(ChainModel chain, char? sign, NeCommandModel model)
    {
      if (sign == null)
        return;

      if (sign == '+')
        model.ElementEnablingType.Add((chain, ElementEnabling.Type.Direct));
      else if (sign == '-')
        model.ElementEnablingType.Add((chain, ElementEnabling.Type.Reverse));
    }
  }
}