using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr;
using Ask.Engine.ControlCommandAnalyser.Parser.Helpers;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Processors.Ot
{
  internal class OtTimeProcessor : IParameterProcessor<OtCommandModel>
  {
    public string Process(OtCommandModel model, string remainder, ParameterContext ctx)
    {
      var (time, unitTime, rest) = CommonParameterParser.TimeParser.ParseTime(remainder);

      double? timeValue = TimeManager.GetTime(model, time, unitTime);

      if (timeValue.HasValue && timeValue > -1)
        model.Time = timeValue.Value;

      model.TimeSource = time + unitTime;

      return rest;
    }
  }
}
