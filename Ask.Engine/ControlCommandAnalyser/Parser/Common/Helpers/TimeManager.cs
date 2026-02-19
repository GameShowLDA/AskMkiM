using Ask.Core.Services.Errors.Translation;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers
{
  public static class TimeManager
  {
    public static double? GetTime(BaseCommandModel model, string time, string unitTime)
    {
      double? timeValue = -1;
      if (!string.IsNullOrEmpty(time) && time != null)
      {
        timeValue = CommonParameterParser.ParseToDouble(time);
      }
      else if (!string.IsNullOrEmpty(unitTime))
      {
        timeValue = 1;
        model.Warnings.Add(GeneralWarnings.DefaultTime(model.StartLineNumber, $"{model.CommandNumber} {model.Mnemonic}", $"{timeValue}{unitTime}"));
      }
      return timeValue;
    }
  }
}
