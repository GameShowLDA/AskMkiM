using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Ks;
using static Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.ProcessorFactory;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline
{
  internal class KsParameterPipeline
  {
    private static readonly ParameterPipeline<KsCommandModel> _pipeline =
        new(new IParameterProcessor<KsCommandModel>[]
        {
            Keys<KsCommandModel>(),
            new KsResistanceProcessor(),
        });

    public static string Execute(
        KsCommandModel model,
        string remainder,
        ParameterContext ctx,
        IFastMeter meter)
        => _pipeline.Execute(model, remainder, ctx with { Fastmeter = meter });
  }
}

