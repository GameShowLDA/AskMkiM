using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;
using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr;
using Ask.Engine.ControlCommandAnalyser.Parser.Helpers;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Ie
{
  /// <summary>
  /// Парсер для команд ИЕ (измерение емкости).
  /// </summary>
  [AllowedKeys(Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Д)]
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

      var rmCommandModel = CheckPoints.CheckRm(model, numberLine, commandNumber, mnemonic);
      if (!SourceLinesManager.Check(model, lines, numberLine))
      {
        return model;
      }
      var remainder = PreprocessSourceLines.GetClearCommandBody(model, lines);

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

      model = CapacityManager.ProcessCapacity(model, lowerLimitCapacity, higherLimitCapacity, unit, numberLine, commandNumber, mnemonic);

      if (InvalidParametersOrderManager.HasInvalidParameterOrder(remainder, model.AlgorithmKey, lowerLimitCapacity ?? higherLimitCapacity, out string err))
      {
        model.Errors.Add(GeneralErrors.InvalidParameterOrder(mnemonic, numberLine, $"{commandNumber} {mnemonic}", err));
        LogWarning($"Ошибка порядка параметров (строка {numberLine}): {err}");
        return model;
      }

      model.Scheme = SchemeManager.GetScheme(model, rmCommandModel, numberLine, ref remainder);
      UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder, lowerLimitCapacity, higherLimitCapacity);
      AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

      LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }
  }
}
