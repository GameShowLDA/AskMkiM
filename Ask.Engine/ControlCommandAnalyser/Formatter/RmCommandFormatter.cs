using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Engine.ControlCommandAnalyser.Formatter.Base;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  public class RmCommandFormatter : CommandFormatter<RmCommandModel>
  {
    protected override IEnumerable<string> Format(RmCommandModel rm)
    {
      foreach (var line in FormatCommandStart(rm, includeKey: false))
      {
        yield return line;
      }

      foreach (var line in FormatComments(rm))
      {
        yield return line;
      }

      foreach (var line in FormatPoints(rm))
      {
        yield return line;
      }

      foreach (var line in FormatEnd())
      {
        yield return line;
      }
    }

    private static IEnumerable<string> FormatPoints(RmCommandModel rm)
    {
      if (rm.Parts.Count > 0)
      {
        foreach (var part in rm.Parts)
        {
          if (rm.Parts.Count > 1 || part.PartNumber.HasValue)
          {
            yield return part.PartNumber.HasValue
              ? $"\t* Ч={part.PartNumber.Value}"
              : "\t*";
          }

          foreach (var pair in part.Pairs)
          {
            yield return $"\t{FormatPair(rm, pair)}";
          }
        }

        yield break;
      }

      foreach (var pair in rm.PointsMap)
      {
        yield return $"\t{pair.Key} = {FormatAskPoint(pair.Value)}";
      }
    }

    private static string FormatPair(RmCommandModel rm, RmPairModel pair)
    {
      var left = string.IsNullOrWhiteSpace(pair.Synonym)
        ? pair.OkPoint
        : $"{pair.OkPoint} == {pair.Synonym}";

      var askPoint = rm.PointsMap.TryGetValue(pair.OkPoint, out var mappedPoint)
        ? mappedPoint
        : pair.AskInput;

      return $"{left} = {FormatAskPoint(askPoint)}";
    }

    private static string FormatAskPoint(string askPoint)
    {
      return ExecutionConfig.GetIsLegacyCompatibilityModeEnabled()
        ? $"{LegacyCompatibilityMapper.GetCompatibilityPointByRealAddress(askPoint)}({askPoint})"
        : askPoint;
    }
  }
}
