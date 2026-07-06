using Ask.Engine.ControlCommandAnalyser.Formatter.Base;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  public class PiCommandFormatter : CommandFormatter<PiCommandModel>
  {
    protected override IEnumerable<string> Format(PiCommandModel pi)
    {
      foreach (var line in FormatCommandStart(pi, includeKey: false))
      {
        yield return line;
      }

      var si = pi.SiCommand;
      if (si != null)
      {
        yield return "\tПараметры команды СИ:";
        yield return FormatKeys(si);
        yield return VoltageFormatter.FormatVoltage(si);
        yield return TimeFormatter.FormatTime(si, "Время выполнения", "\t\t");
        yield return ResistanceFormatter.FormatResistance(si);

        yield return "\tПараметры команды ПИ:";
        yield return FormatKeys(pi);
        yield return VoltageFormatter.FormatVoltage(pi);
        yield return VoltageFormatter.FormatVoltageType(pi.VoltageType);
        yield return TimeFormatter.FormatTime(pi, "Время выполнения");

        foreach (var line in FormatComments(pi))
        {
          yield return line;
        }

        foreach (var line in FormatSchemeWithRmCheckDisconnectedPoints(pi, "\tРазобщенные точки:"))
        {
          yield return line;
        }
      }

      foreach (var line in FormatEnd())
      {
        yield return line;
      }
    }
  }
}
