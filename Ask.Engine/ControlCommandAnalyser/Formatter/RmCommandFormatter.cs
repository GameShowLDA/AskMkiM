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
      if (model is RmCommandModel rm)
      {
        yield return $"{rm.CommandNumber} {rm.Mnemonic}";

        if (rm.Comment.Count > 0)
        {
          yield return $"\tКомментарии:";
          foreach (var line in rm.Comment)
          {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed))
              yield return $"\t\t{trimmed}";
          }
        }

        foreach (var pair in rm.PointsMap)
        {
          if (!ExecutionConfig.GetIsLegacyCompatibilityModeEnabled())
          {
            yield return $"\t{pair.Key} = {pair.Value}";
          }
          else
          { 
            yield return $"\t{pair.Key} = {LegacyCompatibilityMapper.GetCompatibilityPointByRealAddress(pair.Value)}({pair.Value})";
          }
        }

        yield return string.Empty;
      }
      else
      {
        yield break;
      }

    }
  }
}
