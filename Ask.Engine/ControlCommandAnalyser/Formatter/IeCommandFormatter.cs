using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Formatter.Base;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  internal class IeCommandFormatter : CommandFormatter<IeCommandModel>
  {
    protected override IEnumerable<string> Format(IeCommandModel ie)
    {
      foreach (var line in FormatCommandStart(ie))
      {
        yield return line;
      }

      yield return CapacityFormatter.FormatCapacityLowerLimit(ie);
      yield return CapacityFormatter.FormatCapacityHigherLimit(ie);

      foreach (var line in FormatComments(ie))
      {
        yield return line;
      }

      foreach (var line in FormatSchemeWithRmCheckConnectedPoints(ie, "\tПроверяемые точки:"))
      {
        yield return line;
      }

      foreach (var line in FormatEnd())
      {
        yield return line;
      }
    }
  }
}
