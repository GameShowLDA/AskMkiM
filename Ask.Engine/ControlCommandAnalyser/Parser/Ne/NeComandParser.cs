using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Pipeline;
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

      var rmCommandModel = CheckPoints.CheckRm(model, numberLine, commandNumber, mnemonic);
      if (!SourceLinesManager.Check(model, lines, numberLine))
        return model;
      var remainder = PreprocessSourceLines.GetClearCommandBody(model, lines);
      remainder = TextRemoveManager.RemoveCommandPrefix(remainder);
      var ctx = ParameterContext.Create(commandNumber, mnemonic, numberLine);
      remainder = NeParameterPipeline.Execute(model, remainder, ctx);
      var scheme = NeSchemeManager.Parse(model, rmCommandModel, numberLine, commandNumber, mnemonic, ref remainder, lines);
      model.Scheme = scheme;
      UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);

      AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

      LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }
  }
}