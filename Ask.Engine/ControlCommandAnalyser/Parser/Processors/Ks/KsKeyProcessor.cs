using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Processors.Ks
{
  internal class KsKeyProcessor : IParameterProcessor<KsCommandModel>
  {
    public string Process(KsCommandModel model, string remainder, ParameterContext ctx)
        => KeyParser.ParseKeys(ctx.LineNumber, model, remainder);
  }
}
