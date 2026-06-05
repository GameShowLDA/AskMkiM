using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Formatter.Base;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  public class KsCommandFormatter : CommandFormatter<KsCommandModel>
  {
    protected override IEnumerable<string> Format(KsCommandModel ks)
    {
      foreach (var line in FormatCommandStart(ks))
      {
        yield return line;
      }

      yield return ResistanceFormatter.FormatResistanceLowerLimit(ks);
      yield return ResistanceFormatter.FormatHigherLimitResistance(ks);

      foreach (var line in FormatComments(ks))
      {
        yield return line;
      }

      foreach (var line in FormatSchemeWithRmCheck(ks, "\tПроверяемые точки:"))
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
