using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Processors.Eht;
using static Ask.Engine.ControlCommandAnalyser.Parser.Processors.ProcessorFactory;


namespace Ask.Engine.ControlCommandAnalyser.Parser.Pipeline
{
  public static class EhtParameterPipeline
  {
    private static readonly ParameterPipeline<EhtCommandModel> _pipeline =
        new(new IParameterProcessor<EhtCommandModel>[]
        {
            Keys<EhtCommandModel>(),
            new EhtResistanceProcessor(),
            Time<EhtCommandModel>(),
        });

    public static string Execute(
        EhtCommandModel model,
        string remainder,
        ParameterContext ctx)
        => _pipeline.Execute(model, remainder, ctx);
  }

}
