using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Ot
{
  internal class OtCommandParser : CommandParserBase<OtCommandModel>
  {
    public override bool CanParse(MnemonicIdentifier mnemonic)
      => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.OT);

    protected override OtCommandModel CreateModel(string commandNumber, int numberLine, List<string> lines) => new()
    {
      CommandNumber = commandNumber,
      SourceLines = lines is null ? new List<string>() : new List<string>(lines),
      StartLineNumber = numberLine,
    };

    protected override string ParseParameters(OtCommandModel model, string remainder, ParameterContext ctx, List<string> lines)
      => OtParameterPipeline.Execute(model, remainder, ctx);

    protected override void ParseStructure(
      OtCommandModel model,
      RmCommandModel rmCommandModel,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines,
      ref string remainder)
      => model.BusPointsDictionary = SchemeManager.GetBusPointsDictionary(
        model,
        rmCommandModel,
        numberLine,
        commandNumber,
        mnemonic,
        ref remainder);

    protected override void HandleUnparsed(OtCommandModel model, int numberLine, string remainder)
      => UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);
  }
}
