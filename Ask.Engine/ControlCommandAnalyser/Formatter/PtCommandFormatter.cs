using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  internal class PtCommandFormatter : ICommandFormatter
  {
    public bool CanFormat(BaseCommandModel model) => model is PtCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      if (model is not PtCommandModel pt)
        yield break;

      var firstLine = $"{pt.CommandNumber} {pt.Mnemonic}";
      yield return firstLine;

      // Ключи команды
      if (pt.AlgorithmKey.Count > 0)
      {
        yield return $"\tКлючи команды: {string.Join(", ", pt.AlgorithmKey)}";
      }
      else
      {
        yield return $"\tКлючи команды не указаны.";
      }

      if (!string.IsNullOrWhiteSpace(pt.TimeSource))
      {
        yield return $"\tВремя подключения точек: {pt.TimeSource}";
      }

      foreach (var bus in pt.BusPointsDictionary)
      {
        yield return $"\tТочки, подключаемые к шине: {bus.Key}";
        foreach (var point in bus.Value)
        {
          yield return $"\t\t{point.Mnemonic} = {point.ToString()}";
        }
      }

      if (pt.Comment.Count > 0)
      {
        yield return $"\tКомметарии:";
        foreach (var line in pt.Comment)
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
