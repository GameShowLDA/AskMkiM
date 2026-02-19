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
namespace Ask.Engine.ControlCommandAnalyser.Parser.Si
{
  internal class SiCommandParser : CommandParserBase<SiCommandModel>
  {
    public override bool CanParse(MnemonicIdentifier mnemonic)
      => mnemonic.Mnemonic.MatchesEnum(MeasurementTypeCommand.SI);

    protected override SiCommandModel CreateModel(string commandNumber, int numberLine, List<string> lines) => new()
    {
      CommandNumber = commandNumber,
      SourceLines = lines is null ? new List<string>() : new List<string>(lines),
      StartLineNumber = numberLine
    };

    protected override bool AfterSourceLinesCheck(
      SiCommandModel model,
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

    protected override ParameterContext CreateContext(
      string commandNumber,
      string mnemonic,
      int numberLine,
      SiCommandModel model)
      => new(commandNumber, mnemonic, numberLine, ServiceLocator.GetRequired<IBreakdownTester>());

    protected override string ParseParameters(SiCommandModel model, string remainder, ParameterContext ctx, List<string> lines)
    {
      var breakdown = ctx.Breakdown;
      if (breakdown == null)
        return remainder;

      return SiParameterPipeline.Execute(model, remainder, ctx, breakdown);
    }

    protected override void ParseStructure(
      SiCommandModel model,
      RmCommandModel rmCommandModel,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines,
      ref string remainder)
      => model.Scheme = SchemeManager.GetScheme(model, rmCommandModel, numberLine, ref remainder);

    protected override void HandleUnparsed(SiCommandModel model, int numberLine, string remainder)
      => UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);
  }
}
