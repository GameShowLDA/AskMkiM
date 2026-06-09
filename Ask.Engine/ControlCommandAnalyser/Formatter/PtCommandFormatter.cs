using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Formatter.Base;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  internal class PtCommandFormatter : CommandFormatter<PtCommandModel>
  {
    protected override IEnumerable<string> Format(PtCommandModel pt)
    {
      foreach (var line in FormatCommandStart(pt))
      {
        yield return line;
      }

      yield return TimeFormatter.FormatTime(pt, "Время подключения точек");

      foreach (var line in FormatBusPointGroups(pt.BusPointsDictionary, "\tТочки, подключаемые к шине"))
      {
        yield return line;
      }

      foreach (var line in FormatComments(pt))
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
