using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  internal class CkCommandFormatter : ICommandFormatter
  {
    public bool CanFormat(BaseCommandModel model) => model is CkCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      if (model is not CkCommandModel ck)
        yield break;

      var firstLine = $"{ck.CommandNumber} {ck.Mnemonic}";
      yield return firstLine;

      yield return $"\tСбрасываемые точки с шин: ";
      foreach (var bus in ck.BusList)
      {
        yield return $"\t\t{bus}";
      }

      if (ck.Comment.Count > 0)
      {
        yield return $"\tКомметрии:";
        foreach (var line in ck.Comment)
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
