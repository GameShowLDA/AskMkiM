using Ask.Core.Services.App;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Pipeline;
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
      var breakdown = ServiceLocator.GetRequired<IBreakdownTester>();

      if (breakdown == null)
      {
        model.Errors.Add(GeneralErrors.FastMeterNotFound(numberLine, $"{commandNumber} {mnemonic}"));
        return model;
      }

      else
      {
        var rmCommandModel = CheckPoints.CheckRm(model, numberLine, commandNumber, mnemonic);

        if (!SourceLinesManager.Check(model, lines, numberLine))
          return model;

        if (lines == null || lines.Count == 0)
        {
          LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
          model.Errors.Add(PiErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
          return model;
        }

        var remainder = PreprocessSourceLines.GetClearCommandBody(model, lines);

        remainder = PiSiSplitter.PreNormalize(remainder);

        LogDebug($"Хвост после ПИ: \"{remainder}\"");
        var (siPart, piPart, errs) = PiSiSplitter.SplitSiFromPiStrict(remainder);
        if (errs.Count > 0)
        {
          LogWarning($"Strict WS issues: {string.Join(" | ", errs)}");
        }

        remainder = TextRemoveManager.RemoveCommandPrefix(remainder);

        var modelSi = new SiCommandModel();
        modelSi.SourceLines = new List<string> { siPart };

        var ctxSi = new ParameterContext(commandNumber, mnemonic, numberLine, breakdown);
        siPart = SiParameterPipeline.Execute(modelSi, siPart, ctxSi, breakdown);
        UnparsedParametersManager.HandleUnparsedParameters(modelSi, numberLine, siPart);

        model.SiCommand = modelSi;
        if (modelSi.Errors.Count > 0)
        {
          model.Errors.AddRange(modelSi.Errors);
        }

        var remainderPi = piPart;
        remainderPi = TextRemoveManager.RemoveCommandPrefix(remainderPi);
        var ctx = new ParameterContext(commandNumber, mnemonic, numberLine, breakdown);
        remainderPi = PiParameterPipeline.Execute(model, remainderPi, ctx, breakdown);
        model.Scheme = SchemeManager.GetScheme(model, rmCommandModel, numberLine, ref remainderPi);
        UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainderPi);

        LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

        model.SiCommand.FormattedStartLineNumber = model.FormattedStartLineNumber;
        model.SiCommand.Scheme = model.Scheme;
        model.SiCommand.CommandNumber = model.CommandNumber;
        model.SiCommand.StartLineNumber = model.StartLineNumber;

        return model;
      }
    }
  }
}
