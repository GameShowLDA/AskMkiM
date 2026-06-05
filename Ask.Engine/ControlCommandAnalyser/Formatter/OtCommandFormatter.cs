using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Formatter.Base;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  public class OtCommandFormatter : CommandFormatter<OtCommandModel>
  {
    protected override IEnumerable<string> Format(OtCommandModel ot)
    {
      foreach (var line in FormatCommandStart(ot))
      {
        yield return line;
      }

      yield return TimeFormatter.FormatTime(ot, "Время отключения точек");

      foreach (var line in FormatBusPointGroups(ot.BusPointsDictionary, "\tТочки, отключаемые от шины"))
      {
        yield return line;
      }

      foreach (var line in FormatComments(ot))
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
