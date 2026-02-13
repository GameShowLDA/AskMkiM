using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Processors.Si;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Pipeline
{
  internal static class SiParameterPipeline
  {
    private static readonly ParameterPipeline<SiCommandModel> _pipeline =
        new(new IParameterProcessor<SiCommandModel>[]
        {
            new SiKeyProcessor(),
            new SiVoltageProcessor(),
            new SiResistanceProcessor(),
            new SiTimeProcessor()
        });

    public static string Execute(
        SiCommandModel model,
        string remainder,
        ParameterContext ctx,
        IBreakdownTester breakdown)
        => _pipeline.Execute(model, remainder, ctx with { Breakdown = breakdown });
  }

}
