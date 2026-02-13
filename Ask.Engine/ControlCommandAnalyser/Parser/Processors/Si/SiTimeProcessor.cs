using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Processors.Si
{
  internal class SiTimeProcessor : IParameterProcessor<SiCommandModel>
  {
    public string Process(SiCommandModel model, string remainder, ParameterContext ctx)
    {
      var (time, unit, rest) =
          CommonParameterParser.TimeParser.ParseTime(remainder);

      double value = string.IsNullOrWhiteSpace(time)
          ? 5
          : CommonParameterParser.ParseToDouble(time);

      model.Time = value;
      model.TimeSource = $"{value}{unit}";

      return rest;
    }
  }
}
