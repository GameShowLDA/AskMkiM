using Ask.Core.Services.App;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Pipeline;
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
      LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}");

      var model = new SiCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = lines?.ToList() ?? [],
        StartLineNumber = numberLine
      };

      var rmCommandModel = CheckPoints.CheckRm(model, numberLine, commandNumber, mnemonic);

      if (!SourceLinesManager.Check(model, lines, numberLine))
        return model;

      var breakdown = ServiceLocator.GetRequired<IBreakdownTester>();

      if (breakdown == null)
      {
        model.Errors.Add(GeneralErrors.FastMeterNotFound(numberLine, $"{commandNumber} {mnemonic}"));
        return model;
      }

      var remainder = PreprocessSourceLines.GetClearCommandBody(model, lines);
      remainder = TextRemoveManager.RemoveCommandPrefix(remainder);

      var ctx = new ParameterContext(commandNumber, mnemonic, numberLine, breakdown);

      remainder = SiParameterPipeline.Execute(model, remainder, ctx, breakdown);

      model.Scheme = SchemeManager.GetScheme(model, rmCommandModel, numberLine, ref remainder);

      UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);

      AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

      LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }
  }
}
