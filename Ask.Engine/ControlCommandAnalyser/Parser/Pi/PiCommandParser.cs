using Ask.Core.Services.App;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Pi
{
  internal class PiCommandParser : CommandParserBase<PiCommandModel>
  {
    public override bool CanParse(MnemonicIdentifier mnemonic)
      => mnemonic.Mnemonic.MatchesEnum(MeasurementTypeCommand.PI);

    protected override PiCommandModel CreateModel(string commandNumber, int numberLine, List<string> lines) => new()
    {
      CommandNumber = commandNumber,
      SourceLines = lines is null ? new List<string>() : new List<string>(lines),
      StartLineNumber = numberLine,
    };

    protected override bool BeforeCheckRm(
      PiCommandModel model,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines)
    {
      var breakdown = ServiceLocator.GetRequired<IBreakdownTester>();
      if (breakdown == null)
      {
        model.Errors.Add(GeneralErrors.FastMeterNotFound(numberLine, $"{commandNumber} {mnemonic}"));
        return false;
      }
      return true;
    }

    protected override bool ShouldRemoveCommandPrefix(PiCommandModel model) => false;

    protected override bool ShouldValidateAllowedKeys(PiCommandModel model) => false;

    protected override ParameterContext CreateContext(
      string commandNumber,
      string mnemonic,
      int numberLine,
      PiCommandModel model)
      => new(commandNumber, mnemonic, numberLine, ServiceLocator.GetRequired<IBreakdownTester>());

    protected override string ParseParameters(PiCommandModel model, string remainder, ParameterContext ctx, List<string> lines)
    {
      var breakdown = ctx.Breakdown;
      if (breakdown == null)
        return remainder;

      remainder = PiSiSplitter.PreNormalize(remainder);

      LogDebug($"Хвост после ПИ: \"{remainder}\"");
      var (siPart, piPart, errs) = PiSiSplitter.SplitSiFromPiStrict(remainder);
      if (errs.Count > 0)
      {
        LogWarning($"Strict WS issues: {string.Join(" | ", errs)}");
      }

      remainder = TextRemoveManager.RemoveCommandPrefix(remainder);

      var modelSi = new SiCommandModel
      {
        SourceLines = new List<string> { siPart }
      };

      var ctxSi = new ParameterContext(ctx.CommandNumber, ctx.Mnemonic, ctx.LineNumber, breakdown);
      siPart = SiParameterPipeline.Execute(modelSi, siPart, ctxSi, breakdown);
      UnparsedParametersManager.HandleUnparsedParameters(modelSi, ctx.LineNumber, siPart);

      model.SiCommand = modelSi;
      if (modelSi.Errors.Count > 0)
      {
        model.Errors.AddRange(modelSi.Errors);
      }

      var remainderPi = TextRemoveManager.RemoveCommandPrefix(piPart);
      var ctxPi = new ParameterContext(ctx.CommandNumber, ctx.Mnemonic, ctx.LineNumber, breakdown);
      return PiParameterPipeline.Execute(model, remainderPi, ctxPi, breakdown);
    }

    protected override void ParseStructure(
      PiCommandModel model,
      RmCommandModel rmCommandModel,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines,
      ref string remainder)
      => model.Scheme = SchemeManager.GetScheme(model, rmCommandModel, numberLine, ref remainder);

    protected override void HandleUnparsed(PiCommandModel model, int numberLine, string remainder)
      => UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);

    protected override void AfterParse(
      PiCommandModel model,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines)
    {
      if (model.SiCommand == null)
        return;

      model.SiCommand.FormattedStartLineNumber = model.FormattedStartLineNumber;
      model.SiCommand.Scheme = model.Scheme;
      model.SiCommand.CommandNumber = model.CommandNumber;
      model.SiCommand.StartLineNumber = model.StartLineNumber;
    }
  }
}
