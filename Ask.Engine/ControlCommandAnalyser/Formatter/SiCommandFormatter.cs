using Ask.Engine.ControlCommandAnalyser.Formatter.Base;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  public class SiCommandFormatter : CommandFormatter<SiCommandModel>
  {
    protected override IEnumerable<string> Format(SiCommandModel si)
    {
      foreach (var line in FormatCommandStart(si))
      {
        yield return line;
      }

      yield return VoltageFormatter.FormatVoltage(si);
      yield return TimeFormatter.FormatTime(si, "Время ожидания НОРМЫ");
      yield return ResistanceFormatter.FormatResistance(si);

      foreach (var line in FormatComments(si))
      {
        yield return line;
      }

      foreach (var line in FormatSchemeWithRmCheck(si, "\tРазобщенные точки:"))
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
