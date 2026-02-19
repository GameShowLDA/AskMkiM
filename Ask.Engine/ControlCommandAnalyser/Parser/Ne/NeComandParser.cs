using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Ne
{
  internal class NeComandParser : CommandParserBase<NeCommandModel>
  {
    public override bool CanParse(MnemonicIdentifier mnemonic)
      => mnemonic.Mnemonic.MatchesEnum(MeasurementTypeCommand.NE);

    protected override NeCommandModel CreateModel(string commandNumber, int numberLine, List<string> lines) => new()
    {
      CommandNumber = commandNumber,
      SourceLines = lines is null ? new List<string>() : new List<string>(lines),
      StartLineNumber = numberLine,
    };

    protected override string ParseParameters(NeCommandModel model, string remainder, ParameterContext ctx, List<string> lines)
      => NeParameterPipeline.Execute(model, remainder, ctx);

    protected override void ParseStructure(
      NeCommandModel model,
      RmCommandModel rmCommandModel,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines,
      ref string remainder)
      => model.Scheme = NeSchemeManager.Parse(model, rmCommandModel, numberLine, commandNumber, mnemonic, ref remainder, lines);

    protected override void HandleUnparsed(NeCommandModel model, int numberLine, string remainder)
      => UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);
  }
}
