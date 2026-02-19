using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors
{
  public static class ProcessorFactory
  {
    public static IParameterProcessor<TModel> Keys<TModel>()
        where TModel : BaseCommandModel
        => new KeyProcessor<TModel>();

    public static IParameterProcessor<TModel> Time<TModel>()
        where TModel : BaseCommandModel, ITimeCommandModel
        => new TimeProcessor<TModel>();
  }
}
