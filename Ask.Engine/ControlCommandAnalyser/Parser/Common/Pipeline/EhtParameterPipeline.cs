using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Eht;
using static Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.ProcessorFactory;


namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline
{
  public static class EhtParameterPipeline
  {
    private static readonly ParameterPipeline<EhtCommandModel> _pipeline =
        new(new IParameterProcessor<EhtCommandModel>[]
        {
            Keys<EhtCommandModel>(),
            new EhtResistanceProcessor(),
            new EhtAmperageProcessor(),
            Time<EhtCommandModel>(),
        });

    public static string Execute(
        EhtCommandModel model,
        string remainder,
        ParameterContext ctx)
        => _pipeline.Execute(model, remainder, ctx);
  }

}
