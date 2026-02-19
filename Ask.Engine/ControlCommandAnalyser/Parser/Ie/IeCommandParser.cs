using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;
using Ask.Engine.ControlCommandAnalyser.Parser.Common;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Ie
{
  /// <summary>
  /// Парсер для команд ИЕ (измерение емкости).
  /// </summary>
  [AllowedKeys(Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Д)]
  internal class IeCommandParser : CommandParserBase<IeCommandModel>
  {
    public override bool CanParse(MnemonicIdentifier mnemonic)
      => mnemonic.Mnemonic.MatchesEnum(MeasurementTypeCommand.IE);

    protected override IeCommandModel CreateModel(string commandNumber, int numberLine, List<string> lines) => new()
    {
      CommandNumber = commandNumber,
      SourceLines = lines is null ? new List<string>() : new List<string>(lines),
      StartLineNumber = numberLine,
    };

    protected override string ParseParameters(IeCommandModel model, string remainder, ParameterContext ctx, List<string> lines)
      => IeParameterPipeline.Execute(model, remainder, ctx);

    protected override bool AfterParametersParsed(
      IeCommandModel model,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines,
      ref string remainder)
    {
      if (InvalidParametersOrderManager.HasInvalidParameterOrder(
            remainder,
            model.AlgorithmKey,
            model.LowerLimitCapacity?.ToString(),
            out string err))
      {
        model.Errors.Add(GeneralErrors.InvalidParameterOrder(mnemonic, numberLine, $"{commandNumber} {mnemonic}", err));
        LogWarning($"Ошибка порядка параметров (строка {numberLine}): {err}");
        return false;
      }

      return true;
    }

    protected override void ParseStructure(
      IeCommandModel model,
      RmCommandModel rmCommandModel,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines,
      ref string remainder)
      => model.Scheme = SchemeManager.GetScheme(model, rmCommandModel, numberLine, ref remainder);

    protected override void HandleUnparsed(IeCommandModel model, int numberLine, string remainder)
      => UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);
  }
}
