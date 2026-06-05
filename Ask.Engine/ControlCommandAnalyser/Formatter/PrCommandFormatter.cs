using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Formatter.Base;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  internal class PrCommandFormatter : CommandFormatter<PrCommandModel>
  {
    protected override IEnumerable<string> Format(PrCommandModel pr)
    {
      foreach (var line in FormatCommandStart(pr))
      {
        yield return line;
      }

      foreach (var line in FormatComments(pr))
      {
        yield return line;
      }

      yield return TimeFormatter.FormatTime(pr, "Время выдержки");

      if (!HasRmModel())
      {
        yield return "\tМодель РМ не задана!";
        yield break;
      }

      if (pr.Scheme == null || pr.Scheme.IsEmpty())
      {
        yield return "\t\tТочки не заданы!";
        yield break;
      }

      if (pr.Scheme.GroupModels.Count > 0 && !pr.AlgorithmKey.Contains(AlgorithmKey.ЗС.ToString()))
      {
        yield return "\tПроверка на сообщение:";
        yield return ResistanceFormatter.FormatResistanceLowerLimit(pr.ConnectedLowerLimitResistanceSource);
        yield return ResistanceFormatter.FormatHigherLimitResistance(pr.ConnectedHigherLimitResistanceSource);
        yield return "\t\tЗаданные точки:";

        foreach (var line in SchemeFormatter.FormatConnectedChains(pr.Scheme))
        {
          yield return line;
        }
      }

      if (pr.Scheme.GroupModels.Count > 0 && !pr.AlgorithmKey.Contains(AlgorithmKey.ЗР.ToString()))
      {
        yield return "\tПроверка на разобщение:";
        yield return ResistanceFormatter.FormatResistanceLowerLimit(pr.DisconnectedLowerLimitResistanceSource);
        yield return ResistanceFormatter.FormatHigherLimitResistance(pr.DisconnectedHigherLimitResistanceSource);
        yield return "\t\tЗаданные точки:";

        foreach (var line in SchemeFormatter.FormatDisconnectedPoints(pr.Scheme))
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
