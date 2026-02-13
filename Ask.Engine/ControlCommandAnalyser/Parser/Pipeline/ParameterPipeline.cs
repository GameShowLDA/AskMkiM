using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Pipeline
{
  public class ParameterPipeline<TModel>
  {
    private readonly IReadOnlyList<IParameterProcessor<TModel>> _processors;

    public ParameterPipeline(IEnumerable<IParameterProcessor<TModel>> processors)
    {
      _processors = processors.ToList();
    }

    public string Execute(TModel model, string remainder, ParameterContext ctx)
    {
      foreach (var processor in _processors)
      {
        remainder = processor.Process(model, remainder, ctx);
      }

      return remainder;
    }
  }
}
