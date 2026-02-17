using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using Ask.Engine.ControlCommandAnalyser.Parser.Processors.Pr;
using static Ask.Engine.ControlCommandAnalyser.Parser.Processors.ProcessorFactory;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Pipeline
{
  internal class PrParameterPipeline
  {
    private static readonly ParameterPipeline<PrCommandModel> _pipeline =
        new(new IParameterProcessor<PrCommandModel>[]
        {
            Keys<PrCommandModel>(),
            new PrResistanceProcessor(),
            Time<PrCommandModel>(),
        });

    public static string Execute(
        PrCommandModel model,
        string remainder,
        ParameterContext ctx,
        IFastMeter meter)
        => _pipeline.Execute(model, remainder, ctx with { Fastmeter = meter });
  }
}
