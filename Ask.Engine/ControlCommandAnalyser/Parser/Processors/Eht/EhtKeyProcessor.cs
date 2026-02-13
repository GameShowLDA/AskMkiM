using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Processors.Eht
{
  public class EhtKeyProcessor : IParameterProcessor<EhtCommandModel>
  {
    public string Process(EhtCommandModel model, string remainder, ParameterContext ctx)
        => KeyParser.ParseKeys(ctx.LineNumber, model, remainder);
  }
}
