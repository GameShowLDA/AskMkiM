using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Processors.Pi;
using Ask.Engine.ControlCommandAnalyser.Parser.Processors.Si;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Pipeline
{
  internal class PiParameterPipeline
  {
    private static readonly ParameterPipeline<PiCommandModel> _pipeline =
        new(new IParameterProcessor<PiCommandModel>[]
        {
            new PiKeyProcessor(),
            new PiVoltageProcessor(),
            new PiTimeProcessor()
        });

    public static string Execute(
        PiCommandModel model,
        string remainder,
        ParameterContext ctx,
        IBreakdownTester breakdown)
        => _pipeline.Execute(model, remainder, ctx with { Breakdown = breakdown });
  }
}
