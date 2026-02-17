using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;
using Ask.Engine.ControlCommandAnalyser.Parser.Processors.Pi;
using static Ask.Engine.ControlCommandAnalyser.Parser.Processors.ProcessorFactory;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Pipeline
{
  internal class PiParameterPipeline
  {
    private static readonly ParameterPipeline<PiCommandModel> _pipeline =
        new(new IParameterProcessor<PiCommandModel>[]
        {
            Keys<PiCommandModel>(),
            new PiVoltageProcessor(),
            Time<PiCommandModel>(),
        });

    public static string Execute(
        PiCommandModel model,
        string remainder,
        ParameterContext ctx,
        IBreakdownTester breakdown)
        => _pipeline.Execute(model, remainder, ctx with { Breakdown = breakdown });
  }
}
