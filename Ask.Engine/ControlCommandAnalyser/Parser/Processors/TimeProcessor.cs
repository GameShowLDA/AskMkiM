using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr;
using Ask.Engine.ControlCommandAnalyser.Parser.Helpers;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Processors
{
  internal class TimeProcessor<TModel> : IParameterProcessor<TModel>
      where TModel : BaseCommandModel, ITimeCommandModel
  {
    public string Process(TModel model, string remainder, ParameterContext ctx)
    {
      var (timeRaw, unitRaw, rest) =
          CommonParameterParser.TimeParser.ParseTime(remainder);

      double? timeValue = TimeManager.GetTime(model, timeRaw, unitRaw);

      if (timeValue.HasValue && timeValue > -1)
        model.Time = timeValue.Value;

      model.TimeSource = timeRaw + unitRaw;

      return rest;
    }
  }
}
