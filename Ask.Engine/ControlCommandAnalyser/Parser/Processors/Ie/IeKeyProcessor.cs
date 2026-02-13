using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Processors.Ie
{
  internal class IeKeyProcessor : IParameterProcessor<IeCommandModel>
  {
    public string Process(IeCommandModel model, string remainder, ParameterContext ctx)
        => KeyParser.ParseKeys(ctx.LineNumber, model, remainder);
  }
}
