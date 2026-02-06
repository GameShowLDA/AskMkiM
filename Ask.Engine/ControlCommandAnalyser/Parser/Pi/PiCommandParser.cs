using Ask.Core.Services.App;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr;
using Ask.Engine.ControlCommandAnalyser.Parser.Si;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Pi
{
  /// <summary>
  /// Парсер для команды ПИ (пробой изоляции).
  /// </summary>
  public class PiCommandParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic) => mnemonic.Mnemonic.MatchesEnum(MeasurementTypeCommand.PI);

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");

      var model = new PiCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };
      var breakDown = ServiceLocator.GetRequired<IBreakdownTester>();
      if (breakDown == null)
      {
        LogError($"Не найдена пробойная установка.");
        model.Errors.Add(GeneralErrors.BreakDownNotFound(numberLine, $"{commandNumber} {mnemonic}"));
        return model;
      }
      else
      {
        var maxDCWVoltage = MeasurementTypeCommand.PI_DCW.GetDisplayInfo().UpperLimit;
        var minVoltage = MeasurementTypeCommand.PI_ACW.GetDisplayInfo().LowerLimit;
        var maxACWVoltage = MeasurementTypeCommand.PI_ACW.GetDisplayInfo().UpperLimit;

        var rmCommandModel = CommandsModel.GetRMModel();

        if (rmCommandModel == null)
        {
          LogError($"Команда РМ не найдена");
          model.Errors.Add(PiErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
        }

        if (lines == null || lines.Count == 0)
        {
          LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
          model.Errors.Add(PiErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
          return model;
        }

        List<string> processedLines = CommentsParser.ParseComments(lines, model);
        model.SourceLines = model.SourceLines
          .Where(l => !string.IsNullOrWhiteSpace(l))
          .ToList();

        var body = string.Concat(processedLines.Count > 0 && processedLines.FindAll(l => string.IsNullOrEmpty(l) || string.IsNullOrWhiteSpace(l)).Count == 0 ?
          processedLines : model.SourceLines)
          .Replace("\r", "")
          .Replace("\n", "");

        body = PiSiSplitter.PreNormalize(body);

        var head = Regex.Match(body, @"^\s*\d+\s+ПИ\s*(.*)$", RegexOptions.IgnoreCase);
        var remainder = head.Success ? head.Groups[1].Value : body;

        LogDebug($"Хвост после ПИ: \"{remainder}\"");
        var (siPart, piPart, errs) = PiSiSplitter.SplitSiFromPiStrict(body);
        if (errs.Count > 0)
        {
          LogWarning($"Strict WS issues: {string.Join(" | ", errs)}");
        }

        var modelSi = new SiCommandModel();
        modelSi.SourceLines = new List<string> { siPart };
        var siRemainder = SiCommandParser.ManageSiParametersParse(modelSi, commandNumber, mnemonic, numberLine, siPart, breakDown);

        if (!string.IsNullOrEmpty(siRemainder))
        {
          model.UnparsedParameters = "! Не распознанные параметры: ";
          model.UnparsedParameters += siRemainder;
          model.Errors.Add(GeneralErrors.UnrecognizedParameters(siRemainder, numberLine, $"{commandNumber} {mnemonic}"));
        }

        model.SiCommand = modelSi;
        if (modelSi.Errors.Count > 0)
        {
          model.Errors.AddRange(modelSi.Errors);
        }

        var remainderPi = piPart;

        var match2 = Regex.Match(remainderPi, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s*(.*)$");
        if (match2.Success) remainderPi = match2.Groups[1].Value.Trim();

        string? voltage = null, time = null, unit = null, unitTime = null;

        remainderPi = KeyParser.ParseKeys(numberLine, model, remainderPi);

        (voltage, unit, remainderPi) = CommonParameterParser.VoltageParser.ParseVoltage(remainderPi);
        LogDebug($"После парсинга напряжения: voltage='{voltage}{unit}', remainder='{remainderPi}'");

        (time, unitTime, remainderPi) = CommonParameterParser.TimeParser.ParseTime(remainderPi);
        LogDebug($"После парсинга времени: time='{time}{unitTime}', remainder='{remainderPi}'");

        bool isDcw = remainderPi.Contains('+');
        if (isDcw)
        {
          model.VoltageType = VoltageEnum.Type.DCW;
          remainderPi = remainderPi.Replace("+", string.Empty);
        }
        else
        {
          model.VoltageType = VoltageEnum.Type.ACW;
        }

        model.VoltageSource = voltage;

        if (model.VoltageSource != null)
        {
          model.Voltage = CommonParameterParser.ParseToDouble(model.VoltageSource);
          model.VoltageSource += unit;
          var maxVoltage = maxACWVoltage;
          var voltageType = string.Empty;
          if (model.VoltageType == VoltageEnum.Type.DCW)
          {
            maxVoltage = maxDCWVoltage;
          }
          voltageType = model.VoltageType == VoltageEnum.Type.DCW ? "постоянного" : "переменного";
          var voltageValue = UnitsConvertor.TryConvertBack(model.Voltage.Value, unit);
          if (model.Voltage.Value > maxVoltage)
          {
            var maxValue = UnitsConvertor.TryConvertBack(maxVoltage, "В");
            LogError($"В команде ПИ указано напряжение, превышающее максимально допустимое напряжение пробойной установки.");
            var description = $"В команде {commandNumber} {mnemonic} указано напряжение ({voltageValue.Item1} {voltageValue.Item2}), " +
              $"превышающий максимально допустимое напряжение пробойной установки ({maxValue.Item1} {maxValue.Item2}  " +
              $"для {voltageType} тока).";
            model.Errors.Add(GeneralErrors.VoltageConflict(numberLine, $"{commandNumber} {mnemonic}", description));
          }
          else if (model.Voltage.Value < minVoltage)
          {
            var minValue = UnitsConvertor.TryConvertBack(minVoltage, "В");
            LogError($"В команде ПИ указано напряжение, меньше минимально допустимого напряжения пробойной установки.");
            var description = $"В команде {commandNumber} {mnemonic} указано напряжение ({voltageValue.Item1} {voltageValue.Item2}), " +
              $"меньше минимально допустимого напряжения пробойной установки ({minValue.Item1} {minValue.Item2}" +
              $"для {voltageType} тока).";
            model.Errors.Add(GeneralErrors.VoltageConflict(numberLine, $"{commandNumber} {mnemonic}", description));
          }
          else
          {
            model.Voltage = model.Voltage.Value;
            model.VoltageSource = model.Voltage.Value.ToString() + unit;
          }
        }
        else
        {
          model.Voltage = minVoltage;
          model.VoltageSource = model.Voltage.Value.ToString() + "В";
          LogDebug($"В команде ПИ не указано напряжение. Установлено значение по умолчанию {minVoltage} В.");
          model.Warnings.Add(GeneralWarnings.DefaultVoltage(model.StartLineNumber, $"{commandNumber} {mnemonic}", model.VoltageSource));
        }

        model.Time = string.IsNullOrEmpty(time) || time == null ? 1 : CommonParameterParser.ParseToDouble(time);
        if (string.IsNullOrEmpty(time) || time == null)
        {
          model.TimeSource = "1c";
          model.Warnings.Add(GeneralWarnings.DefaultTime(model.StartLineNumber, $"{commandNumber} {mnemonic}", model.TimeSource));
        }
        else
        {
          model.TimeSource = time + unitTime;
        }

        if (model.Voltage == null)
        {
          model.Errors.Add(PiErrors.CannotParseParameters("Не указано напряжение", numberLine, $"{commandNumber} {mnemonic}"));
          LogWarning($"Не указано напряжение (строка {numberLine}): {commandNumber} {mnemonic}");
        }

        if (model.Time == null)
        {
          model.Errors.Add(PiErrors.CannotParseParameters("Не указано время", numberLine, $"{commandNumber} {mnemonic}"));
          LogWarning($"Не указано время (строка {numberLine}): {commandNumber} {mnemonic}");
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

          int idxStarInFirstLine = remainderPi.IndexOf('*');
          int idxStarInSecondLine = remainderPi.LastIndexOf('*');
          if (idxStarInFirstLine >= 0 && idxStarInSecondLine > idxStarInFirstLine)
          {
            remainderPi =
                remainderPi[..idxStarInFirstLine].Trim()
                + remainderPi[(idxStarInSecondLine + 1)..].Trim();
          }
          else
          {
            remainderPi = remainderPi.Trim();
          }
          if (model.SiCommand.AlgorithmKey.Contains(AlgorithmKey.П.ToString())
            || model.AlgorithmKey.Contains(AlgorithmKey.П.ToString()))
          {
            // находим цепи точек из предыдущей команды проверки
            var newScheme = CommandsModel.CheckKeyP(model, model.Scheme, model.SiCommand);
            if (newScheme != null)
            {
              model.Scheme = newScheme;
              model.SiCommand.Scheme = model.Scheme;
            }
            else
            {
              model.Errors.Add(PiErrors.PreviousCommandHasNoPoints(numberLine, $"{commandNumber} {mnemonic}"));
            }
          }
          else if (model.SiCommand.AlgorithmKey.Contains(AlgorithmKey.С.ToString())
            || model.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
          {
            model.Scheme = CommandsModel.CheckKeyS(model.Scheme);
            model.SiCommand.Scheme = model.Scheme;
          }
        }
        else if (model.SiCommand.AlgorithmKey.Contains(AlgorithmKey.П.ToString())
          || model.AlgorithmKey.Contains(AlgorithmKey.П.ToString()))
        {
          // находим цепи точек из предыдущей команды проверки
          var newScheme = CommandsModel.CheckKeyP(model, model.Scheme, model.SiCommand);
          if (newScheme != null)
          {
            model.Scheme = newScheme;
            model.SiCommand.Scheme = model.Scheme;
          }
          else
          {
            model.Errors.Add(PiErrors.PreviousCommandHasNoPoints(numberLine, $"{commandNumber} {mnemonic}"));
          }
        }
        else if (model.SiCommand.AlgorithmKey.Contains(AlgorithmKey.С.ToString())
          || model.AlgorithmKey.Contains(AlgorithmKey.С.ToString()))
        {
          model.Scheme = CommandsModel.CheckKeyS(model.Scheme);
          model.SiCommand.Scheme = model.Scheme;
        }
        else
        {
          LogWarning($"Во всём теле команды не найден блок точек '*...*' (строка {numberLine}): {commandNumber} {mnemonic}");
          model.Errors.Add(PiErrors.EmptyPoints(numberLine, $"{commandNumber} {mnemonic}"));
        }

        CheckUnparsedParameters(commandNumber, mnemonic, numberLine, model, remainderPi);
        if (model.AlgorithmKey.Count == 0
          && model.SiCommand.AlgorithmKey.Count != 0
          && model.AlgorithmKey != null
          && model.SiCommand.AlgorithmKey != null)
        {
          model.AlgorithmKey = model.SiCommand.AlgorithmKey;
        }

        LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

        model.SiCommand.CommandNumber = model.CommandNumber;
        model.SiCommand.FormattedStartLineNumber = model.FormattedStartLineNumber;
        model.SiCommand.Scheme = model.Scheme;
        model.SiCommand.StartLineNumber = model.StartLineNumber;

        return model;
      }
    }

    private static void CheckUnparsedParameters(string commandNumber, string mnemonic, int numberLine, PiCommandModel model, string remainderPi)
    {
      if (!string.IsNullOrEmpty(remainderPi))
      {
        model.UnparsedParameters = "! Не распознанные параметры: ";
        model.UnparsedParameters += remainderPi;
        model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainderPi, numberLine, $"{commandNumber} {mnemonic}"));
      }
    }
  }
}
