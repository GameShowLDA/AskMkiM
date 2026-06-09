using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Formatter.Base;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  internal class EhtCommandFormatter : CommandFormatter<EhtCommandModel>
  {
    protected override IEnumerable<string> Format(EhtCommandModel eht)
    {
      foreach (var line in FormatCommandStart(eht))
      {
        yield return line;
      }

      yield return ResistanceFormatter.FormatResistanceLowerLimit(eht);
      yield return ResistanceFormatter.FormatHigherLimitResistance(eht);
      yield return ResistanceFormatter.FormatCableResistance(eht);

      foreach (var line in FormatComments(eht))
      {
        yield return line;
      }

      yield return TimeFormatter.FormatTime(eht, "Время выдержки");

      foreach (var line in FormatSchemeWithRmCheck(eht, "\tПроверяемые точки:", "\tМодель РМ не задана!"))
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
