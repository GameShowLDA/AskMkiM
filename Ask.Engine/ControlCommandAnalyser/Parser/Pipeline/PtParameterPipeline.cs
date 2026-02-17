using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using static Ask.Engine.ControlCommandAnalyser.Parser.Processors.ProcessorFactory;


namespace Ask.Engine.ControlCommandAnalyser.Parser.Pipeline
{
  internal class PtParameterPipeline
  {
    private static readonly ParameterPipeline<PtCommandModel> _pipeline =
        new(new IParameterProcessor<PtCommandModel>[]
        {
          Keys<PtCommandModel>(),
          Time<PtCommandModel>(),
        });

    public static string Execute(
        PtCommandModel model,
        string remainder,
        ParameterContext ctx)
        => _pipeline.Execute(model, remainder, ctx);
  }
}

