using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;
using Ask.Engine.ControlCommandAnalyser.Parser.Processors.Ie;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Pipeline
{
  internal class IeParameterPipeline
  {
    private static readonly ParameterPipeline<IeCommandModel> _pipeline =
        new(new IParameterProcessor<IeCommandModel>[]
        {
          new IeKeyProcessor(),
            new IeCapacityProcessor(),
        });

    public static string Execute(
        IeCommandModel model,
        string remainder,
        ParameterContext ctx)
        => _pipeline.Execute(model, remainder, ctx);
  }
}
