using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Processors.Eht;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Pipeline
{
  public static class EhtParameterPipeline
  {
    private static readonly ParameterPipeline<EhtCommandModel> _pipeline =
        new(new IParameterProcessor<EhtCommandModel>[]
        {
            new EhtKeyProcessor(),
            new EhtResistanceProcessor(),
            new EhtTimeProcessor()
        });

    public static string Execute(
        EhtCommandModel model,
        string remainder,
        ParameterContext ctx)
        => _pipeline.Execute(model, remainder, ctx);
  }

}
