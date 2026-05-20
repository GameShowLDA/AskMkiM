using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  public class RmCommandFormatter : ICommandFormatter
  {
    public bool CanFormat(BaseCommandModel model) => model is RmCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      if (model is not RmCommandModel rm)
        yield break;

      yield return $"{rm.CommandNumber} {rm.Mnemonic}";

      if (rm.Comment.Count > 0)
      {
        yield return "\tКомментарии:";
        foreach (var line in rm.Comment)
        {
          var trimmed = line.Trim();
          if (!string.IsNullOrEmpty(trimmed))
            yield return $"\t\t{trimmed}";
        }
      }

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
            yield return $"\t{FormatPair(rm, pair)}";
        }
      }
      else
      {
        foreach (var pair in rm.PointsMap)
          yield return $"\t{pair.Key} = {FormatAskPoint(pair.Value)}";
      }

      yield return string.Empty;
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
