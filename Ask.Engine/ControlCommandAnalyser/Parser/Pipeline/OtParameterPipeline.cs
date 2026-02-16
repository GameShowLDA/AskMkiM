using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Processors.Ot;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Pipeline
{
  internal class OtParameterPipeline
  {
    private static readonly ParameterPipeline<OtCommandModel> _pipeline =
        new(new IParameterProcessor<OtCommandModel>[]
        {
            new OtKeyProcessor(),
            new OtTimeProcessor()
        });

    public static string Execute(
        OtCommandModel model,
        string remainder,
        ParameterContext ctx)
        => _pipeline.Execute(model, remainder, ctx);
  }
}
