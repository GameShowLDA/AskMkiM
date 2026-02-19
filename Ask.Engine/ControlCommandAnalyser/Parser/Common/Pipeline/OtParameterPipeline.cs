using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using static Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.ProcessorFactory;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline
{
  internal class OtParameterPipeline
  {
    private static readonly ParameterPipeline<OtCommandModel> _pipeline =
        new(new IParameterProcessor<OtCommandModel>[]
        {
            Keys<OtCommandModel>(),
            Time<OtCommandModel>(),
        });

    public static string Execute(
        OtCommandModel model,
        string remainder,
        ParameterContext ctx)
        => _pipeline.Execute(model, remainder, ctx);
  }
}
