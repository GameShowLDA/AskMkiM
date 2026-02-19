using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Ie;
using static Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.ProcessorFactory;


namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline
{
  internal class IeParameterPipeline
  {
    private static readonly ParameterPipeline<IeCommandModel> _pipeline =
        new(new IParameterProcessor<IeCommandModel>[]
        {
            Keys<IeCommandModel>(),
            new IeCapacityProcessor(),
        });

    public static string Execute(
        IeCommandModel model,
        string remainder,
        ParameterContext ctx)
        => _pipeline.Execute(model, remainder, ctx);
  }
}
