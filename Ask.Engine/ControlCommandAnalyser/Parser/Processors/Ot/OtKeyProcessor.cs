using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Processors.Ot
{
  internal class OtKeyProcessor : IParameterProcessor<OtCommandModel>
  {
    public string Process(OtCommandModel model, string remainder, ParameterContext ctx)
        => KeyParser.ParseKeys(ctx.LineNumber, model, remainder);
  }
}
