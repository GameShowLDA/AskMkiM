using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Eht
{
  internal class EhtAmperageProcessor : IParameterProcessor<EhtCommandModel>
  {
    public string Process(EhtCommandModel model, string remainder, ParameterContext ctx)
    {
      var (amperageRaw, unitRaw, rest) =
          CommonParameterParser.AmperageParser.ParseAmperage(remainder);

      if (!string.IsNullOrWhiteSpace(amperageRaw))
      {
        double value = CommonParameterParser.ParseToDouble(amperageRaw);

        //model.Amperage = value;
        //model.AmperageSource = $"{amperageRaw}{unitRaw}";

        //LogDebug($"Распознана сила тока: {model.AmperageSource}");
        LogDebug($"Распознана сила тока: {amperageRaw}{unitRaw}");
      }

      return rest;
    }
  }
}