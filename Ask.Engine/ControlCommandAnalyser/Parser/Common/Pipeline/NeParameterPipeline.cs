using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Ne;
using static Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.ProcessorFactory;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline
{
  internal class NeParameterPipeline
  {
    private static readonly ParameterPipeline<NeCommandModel> _pipeline =
        new(new IParameterProcessor<NeCommandModel>[]
        {
            Keys<NeCommandModel>(),
            new NeVoltageProcessor(),
            new NeAmperageProcessor(),
        });

    public static string Execute(
        NeCommandModel model,
        string remainder,
        ParameterContext ctx)
        => _pipeline.Execute(model, remainder, ctx);
  }
}
