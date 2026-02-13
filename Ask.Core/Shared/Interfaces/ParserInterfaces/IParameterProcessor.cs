using Ask.Core.Shared.ParserContext;

namespace Ask.Core.Shared.Interfaces.ParserInterfaces
{
  public interface IParameterProcessor<TModel>
  {
    string Process(
        TModel model,
        string remainder,
        ParameterContext context);
  }

}
