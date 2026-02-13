using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Processors.Si
{
  internal class SiKeyProcessor : IParameterProcessor<SiCommandModel>
  {
    public string Process(SiCommandModel model, string remainder, ParameterContext ctx)
        => KeyParser.ParseKeys(ctx.LineNumber, model, remainder);
  }
}
