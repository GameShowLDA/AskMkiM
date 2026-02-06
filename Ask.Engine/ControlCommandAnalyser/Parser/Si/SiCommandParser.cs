using Ask.Core.Services.App;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Si
{
  /// <summary>
  /// Парсер для команд СИ (сопротивление изоляции).
  /// </summary>
  public class SiCommandParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic)
    => mnemonic.Mnemonic.MatchesEnum(MeasurementTypeCommand.SI);

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");

      var model = new SiCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = lines?.ToList() ?? new List<string>(),
        StartLineNumber = numberLine,
      };

      if (lines == null || lines.Count == 0)
      {
        LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
        model.Errors.Add(SiErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
      }

      var rmCommandModel = CommandsModel.GetRMModel();

      if (rmCommandModel == null)
      {
        LogError($"Команда РМ не найдена");
        model.Errors.Add(SiErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
      }

      var breakDown = ServiceLocator.GetRequired<IBreakdownTester>();
      if (breakDown == null)
      {
        LogError($"Не найден быстрый измеритель.");
        model.Errors.Add(GeneralErrors.FastMeterNotFound(numberLine, $"{commandNumber} {mnemonic}"));
        return model;
      }
      else
      {
        var commandInfo = EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.SI);

        var maxVoltage = breakDown.SiMaxVoltage;

        string body = AllLinesInOne(model, lines);

        var remainder = body;

        remainder = ManageSiParametersParse(model, commandNumber, mnemonic, numberLine, remainder, breakDown);

        string bodyNoWs = string.Concat(lines.Select(l => Regex.Replace(l ?? string.Empty, @"\s+", "")));

        int firstStar = bodyNoWs.IndexOf('*');
        int lastStar = bodyNoWs.LastIndexOf('*');

        if (firstStar >= 0 && lastStar > firstStar)
        {
          remainder = ParsePoints(commandNumber, mnemonic, numberLine, model, rmCommandModel, remainder, bodyNoWs, firstStar, lastStar);
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
            model.Errors.Add(SiErrors.PreviousCommandHasNoPoints(numberLine, $"{commandNumber} {mnemonic}"));
          }
        }
        else if (model.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
        {
          model.Scheme = CommandsModel.CheckKeyS(model.Scheme);
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

        AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

        LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

        return model;
      }
    }

    private static string ParsePoints(string commandNumber, string mnemonic, int numberLine, SiCommandModel model, RmCommandModel rmCommandModel, string remainder, string bodyNoWs, int firstStar, int lastStar)
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
        model.Errors.Add(SiErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
      }
      else
      {
        model.Scheme = scheme;
        LogInformation($"Схема распознана: цепей={scheme.GroupModels?.Count ?? 0}, частей={scheme.CountParts()}, точек={scheme.CountPoints()}");
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
      if (model.AlgorithmKey.Contains(AlgorithmKey.П.ToString()))
      {
        var newScheme = CommandsModel.CheckKeyP(model, model.Scheme);
        if (newScheme != null)
        {
          model.Scheme = newScheme;
        }
        else
        {
          model.Errors.Add(SiErrors.PreviousCommandHasNoPoints(numberLine, $"{commandNumber} {mnemonic}"));
        }
      }
      else if (model.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
      {
        model.Scheme = CommandsModel.CheckKeyS(model.Scheme);
      }
      return remainder;
    }

    public static string ManageSiParametersParse(SiCommandModel model, string commandNumber, string mnemonic, int numberLine, string remainder, IBreakdownTester breakDown)
    {
      var body = remainder;
      var match = Regex.Match(remainder, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s*(.*)$");
      if (match.Success)
        remainder = match.Groups[1].Value.Trim();

      remainder = KeyParser.ParseKeys(numberLine, model, remainder);
      remainder = ExtractSiParameters(commandNumber, mnemonic, numberLine, model, remainder, breakDown);

      return remainder;
    }

    private static string ExtractSiParameters(string commandNumber, string mnemonic, int numberLine, SiCommandModel model,
      string remainder, IBreakdownTester breakDown)
    {
      var commandInfo = EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.SI);

      var minVoltage = breakDown.IRMinVoltage;
      var maxVoltage = breakDown.SiMaxVoltage;
      double minResistance = commandInfo.LowerLimit;
      double maxResistance = commandInfo.UpperLimit;
      string defaultResistainceunit = commandInfo.Unit;
      string voltage = string.Empty, resistance = string.Empty, time = string.Empty, unit = string.Empty, unitTime = string.Empty, unitResistance = string.Empty;

      (voltage, unit, remainder) = CommonParameterParser.VoltageParser.ParseVoltage(remainder);
      LogDebug($"После парсинга напряжения: voltage='{voltage}{unit}', remainder='{remainder}'");

      (resistance, unitResistance, remainder) = CommonParameterParser.ResistanceParser.ParseResistance(remainder);
      LogDebug($"После парсинга сопротивления: resistance='{resistance}{unitResistance}', remainder='{remainder}'");
      if (!string.IsNullOrEmpty(resistance))
      {
        resistance = UnitsConvertor.ConvertToMOhms(CommonParameterParser.ParseToDouble(resistance), unitResistance).ToString();
      }
      unitResistance = "МОм";

      (time, unitTime, remainder) = CommonParameterParser.TimeParser.ParseTime(remainder);
      LogDebug($"После парсинга времени: time='{time}{unitTime}', remainder='{remainder}'");

      if (voltage != null)
      {
        model.Voltage = CommonParameterParser.ParseToDouble(voltage);
        var voltageValue = UnitsConvertor.TryConvertBack(model.Voltage.Value, unit);

        if (model.Voltage.HasValue)
        {
          if (model.Voltage.Value > maxVoltage)
          {
            var maxValue = UnitsConvertor.TryConvertBack(maxVoltage, "В");

            LogError($"В команде {commandNumber} {mnemonic} указано напряжение ({voltageValue.Item1} {voltageValue.Item2}), " +
               $"превышающее максимально допустимое напряжение пробойной установки ({maxValue.Item1} {maxValue.Item2}).");
            var description = $"В команде {commandNumber} {mnemonic} указано напряжение ({voltageValue.Item1} {voltageValue.Item2}), " +
              $"превышающее максимально допустимое напряжение пробойной установки ({maxValue.Item1} {maxValue.Item2}).";
            model.Errors.Add(GeneralErrors.VoltageConflict(numberLine, $"{commandNumber} {mnemonic}", description));
          }
          else if (model.Voltage.Value < minVoltage)
          {
            var minValue = UnitsConvertor.TryConvertBack(minVoltage, "В");

            LogError($"В команде {commandNumber} {mnemonic} указано напряжение ({model.Voltage.Value}), " +
               $"меньше минимально допустимого напряжения пробойной установки ({minValue.Item1} {minValue.Item2}).");
            var description = $"В команде {commandNumber} {mnemonic} указано напряжение ({model.Voltage.Value}), " +
              $"меньше минимально допустимого напряжения пробойной установки ({minValue.Item1} {minValue.Item2}).";
            model.Errors.Add(GeneralErrors.VoltageConflict(numberLine, $"{commandNumber} {mnemonic}", description));
          }
          else
          {
            model.Voltage = model.Voltage.Value;
            model.VoltageSource = model.Voltage.Value.ToString() + unit;
          }
        }
      }
      else
      {
        LogError($"В команде СИ не указано напряжение.");
        model.Errors.Add(SiErrors.EmptyVoltage(numberLine, $"{commandNumber} {mnemonic}"));
      }

      double? resistanceValue;
      if (string.IsNullOrEmpty(resistance) || resistance == null)
      {
        resistance = "100";
        resistanceValue = 100;
        unitResistance = "МОм";
        LogDebug($"Для сопротивления установлено значение по умолчанию '100<МОм'");
        model.Warnings.Add(GeneralWarnings.DefaultResistainceLowLimit(model.StartLineNumber, $"{commandNumber} {mnemonic}", $"{resistance} {unitResistance}"));
      }
      else
      {
        resistanceValue = CommonParameterParser.ParseToDouble(resistance);
      }

      if (resistanceValue.HasValue)
      {
        if (resistanceValue.Value > maxResistance)
        {
          LogError($"В команде СИ указано сопротивление, превышающее максимально допустимое сопротивление пробойной установки.");
          var description = $"В команде {commandNumber} {mnemonic} указано сопротивление ({resistance} {unitResistance}), " +
            $"превышающий максимально допустимое сопротивление пробойной установки ({maxResistance} {defaultResistainceunit}).";
          model.Errors.Add(SiErrors.ResistanceLimitsConflict(numberLine, $"{commandNumber} {mnemonic}", description));
        }
        else if (resistanceValue.Value < minResistance)
        {
          LogError($"В команде СИ указано сопротивление, меньше минимально допустимого сопротивления пробойной установки.");
          var description = $"В команде {commandNumber} {mnemonic} указано сопротивление ({resistance} {unitResistance}), " +
            $"меньше минимально допустимого напряжения пробойной установки ({minResistance} {defaultResistainceunit}).";
          model.Errors.Add(SiErrors.ResistanceLimitsConflict(numberLine, $"{commandNumber} {mnemonic}", description));
        }
        else
        {
          model.Resistance = resistanceValue.Value;
        }
      }
      model.ResistanceSource = resistance + "<" + unitResistance;
      model.ResistanceUnit = unitResistance;

      double? timeValue;
      if (string.IsNullOrEmpty(time) || time == null)
      {
        LogDebug($"Для времени установлено значение по умолчанию 5 с.'");
        time = "5с";
        timeValue = 5;
        model.Warnings.Add(GeneralWarnings.DefaultTime(model.StartLineNumber, $"{commandNumber} {mnemonic}", time));
      }
      else
      {
        timeValue = CommonParameterParser.ParseToDouble(time);
      }
      if (timeValue.HasValue)
      {
        model.Time = timeValue.Value;
      }
      model.TimeSource = time + unitTime;

      ValidateSiParameters(commandNumber, mnemonic, numberLine, model, voltage, resistance, time);

      return remainder;
    }

    private static void ValidateSiParameters(string commandNumber, string mnemonic, int numberLine, SiCommandModel model, string? voltage, string? resistance, string time)
    {
      if (voltage != null && string.IsNullOrWhiteSpace(voltage))
      {
        model.Errors.Add(SiErrors.CannotParseParameters("Не указано напряжение", numberLine, $"{commandNumber} {mnemonic}"));
        LogWarning($"Не указано напряжение (строка {numberLine}): {commandNumber} {mnemonic}");
      }

      if (string.IsNullOrWhiteSpace(resistance))
      {
        model.Errors.Add(SiErrors.CannotParseParameters("Не указано сопротивление", numberLine, $"{commandNumber} {mnemonic}"));
        LogWarning($"Не указано сопротивление (строка {numberLine}): {commandNumber} {mnemonic}");
      }

      if (string.IsNullOrWhiteSpace(time))
      {
        model.Errors.Add(SiErrors.CannotParseParameters("Не указано время", numberLine, $"{commandNumber} {mnemonic}"));
        LogWarning($"Не указано время (строка {numberLine}): {commandNumber} {mnemonic}");
      }
    }
    
    private static string AllLinesInOne(SiCommandModel model, List<string> lines)
    {
      List<string> processedLines = CommentsParser.ParseComments(lines, model);
      lines.Clear();
      lines.AddRange(processedLines);
      model.SourceLines = model.SourceLines
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .ToList();

      var body = string.Concat(processedLines.Count > 0 && processedLines.FindAll(l => string.IsNullOrEmpty(l) || string.IsNullOrWhiteSpace(l)).Count == 0 ?
        processedLines : model.SourceLines)
        .Replace("\r", "")
        .Replace("\n", "")
        .Replace("\t", "");

      LogDebug($"Нормализованное тело команды (в одну строку): \"{body}\"");
      return body;
    }
  }
}