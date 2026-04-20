using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  internal class OcCommandFormatter : ICommandFormatter
  {
    public bool CanFormat(BaseCommandModel model) => model is OcCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      if (model is not OcCommandModel oc)
        yield break;

      var firstLine = $"{oc.CommandNumber} {oc.Mnemonic}";
      yield return firstLine;

      if (oc.Comment.Count > 0)
      {
        yield return $"\tКомментарии:";
        foreach (var line in oc.Comment)
        {
          var trimmed = line.Trim();
          if (!string.IsNullOrEmpty(trimmed))
            yield return $"\t\t{trimmed}";
        }
      }

      yield return string.Empty;
    }
  }
}

