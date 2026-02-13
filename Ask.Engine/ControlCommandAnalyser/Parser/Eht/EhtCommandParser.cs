using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr;
using Ask.Engine.ControlCommandAnalyser.Parser.Helpers;
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

      var rmCommandModel = CheckPoints.CheckRm(model, numberLine, commandNumber, mnemonic);

      if (!SourceLinesManager.Check(model, lines, numberLine))
      {
        return model;
      }

      var remainder = PreprocessSourceLines.GetClearCommandBody(model, lines);

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

      ResistanceManager.ProcessResistance(model, lowerLimitResistance, higherLimitResistance, cabelLimitResistance, unit, cabelUnit, commandNumber, mnemonic, numberLine);

      double? timeValue = TimeManager.GetTime(model, time, unitTime);

      if (timeValue.HasValue && timeValue > -1)
      {
        model.Time = timeValue.Value;
      }
      model.TimeSource = time + unitTime;

      model.Scheme = SchemeManager.GetScheme(model, rmCommandModel, numberLine, ref remainder);

      UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);
      AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);
      LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }
  }
}
