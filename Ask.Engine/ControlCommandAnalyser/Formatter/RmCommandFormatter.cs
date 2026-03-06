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
          yield return $"\t{pair.Key} = {pair.Value}";

        yield return string.Empty;
      }
      else
      {
        yield break;
      }

    }
  }
}
