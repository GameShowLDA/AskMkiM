using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;
using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr;
using Ask.Engine.ControlCommandAnalyser.Parser.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Pipeline;
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

      remainder = TextRemoveManager.RemoveCommandPrefix(remainder);
      var ctx = ParameterContext.Create(commandNumber, mnemonic, numberLine);
      remainder = IeParameterPipeline.Execute(model, remainder, ctx);

      if (InvalidParametersOrderManager.HasInvalidParameterOrder(remainder, model.AlgorithmKey, model.LowerLimitCapacity?.ToString(), out string err))
      {
        model.Errors.Add(GeneralErrors.InvalidParameterOrder(mnemonic, numberLine, $"{commandNumber} {mnemonic}", err));
        LogWarning($"Ошибка порядка параметров (строка {numberLine}): {err}");
        return model;
      }

      model.Scheme = SchemeManager.GetScheme(model, rmCommandModel, numberLine, ref remainder);
      UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);
      AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

      LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }
  }
}
