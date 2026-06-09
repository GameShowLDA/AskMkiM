using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Formatter.Base;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  internal class CkCommandFormatter : CommandFormatter<CkCommandModel>
  {
    protected override IEnumerable<string> Format(CkCommandModel ck)
    {
      foreach (var line in FormatCommandStart(ck))
      {
        yield return line;
      }

      foreach (var line in SchemeFormatter.FormatBusPoints(ck.BusList))
      {
        yield return line;
      }

      foreach (var line in FormatComments(ck))
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
