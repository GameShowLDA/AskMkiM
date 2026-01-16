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

      foreach(var bus in pt.BusPointsDictionary)
      {
        yield return $"\tТочки, подключаемые к шине: {bus.Key}";
        foreach(var point in bus.Value)
        {
          yield return $"\t\t{point.Item1} = {point.Item2}";
        }
      }
      
      if (pt.Comment.Count > 0)
      {
        yield return $"\tКомметрии:";
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
