using Ask.Core.Services.Translator;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common
{
  internal abstract class CommandParserBase<TModel> : ICommandParser where TModel : BaseCommandModel
  {
    public abstract bool CanParse(MnemonicIdentifier mnemonic);

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");

      var model = CreateModel(commandNumber, numberLine, lines);

      if (!BeforeCheckRm(model, commandNumber, mnemonic, numberLine, lines))
        return model;

      var rmCommandModel = CheckPoints.CheckRm(model, numberLine, commandNumber, mnemonic);

      if (!SourceLinesManager.Check(model, lines, numberLine))
        return model;

      if (!AfterSourceLinesCheck(model, commandNumber, mnemonic, numberLine, lines))
        return model;

      var remainder = PreprocessSourceLines.GetClearCommandBody(model, lines);

      if (ShouldRemoveCommandPrefix(model))
        remainder = TextRemoveManager.RemoveCommandPrefix(remainder);

      var ctx = CreateContext(commandNumber, mnemonic, numberLine, model);
      remainder = ParseParameters(model, remainder, ctx, lines);

      if (!AfterParametersParsed(model, commandNumber, mnemonic, numberLine, lines, ref remainder))
        return model;

      ParseStructure(model, rmCommandModel, commandNumber, mnemonic, numberLine, lines, ref remainder);

      HandleUnparsed(model, numberLine, remainder);

      if (ShouldValidateAllowedKeys(model))
        AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

      LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");
      AfterParse(model, commandNumber, mnemonic, numberLine, lines);

      return model;
    }

    protected abstract TModel CreateModel(string commandNumber, int numberLine, List<string> lines);

    protected virtual bool BeforeCheckRm(
      TModel model,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines) => true;

    protected virtual bool AfterSourceLinesCheck(
      TModel model,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines) => true;

    protected virtual bool ShouldRemoveCommandPrefix(TModel model) => true;

    protected virtual ParameterContext CreateContext(
      string commandNumber,
      string mnemonic,
      int numberLine,
      TModel model) => ParameterContext.Create(commandNumber, mnemonic, numberLine);

    protected abstract string ParseParameters(
      TModel model,
      string remainder,
      ParameterContext ctx,
      List<string> lines);

    protected virtual bool AfterParametersParsed(
      TModel model,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines,
      ref string remainder) => true;

    protected abstract void ParseStructure(
      TModel model,
      RmCommandModel rmCommandModel,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines,
      ref string remainder);

    protected abstract void HandleUnparsed(TModel model, int numberLine, string remainder);

    protected virtual bool ShouldValidateAllowedKeys(TModel model) => true;

    protected virtual void AfterParse(
      TModel model,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines)
    {
    }
  }
}
