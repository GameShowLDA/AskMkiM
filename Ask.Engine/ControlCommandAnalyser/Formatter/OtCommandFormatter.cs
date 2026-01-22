using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  public class OtCommandFormatter : ICommandFormatter
  {
    public bool CanFormat(BaseCommandModel model) => model is OtCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      if (model is not OtCommandModel ot)
        yield break;

      var firstLine = $"{ot.CommandNumber} {ot.Mnemonic}";
      yield return firstLine;

      foreach (var bus in ot.BusPointsDictionary)
      {
        yield return $"\tТочки, отключаемые от шины: {bus.Key}";
        foreach (var point in bus.Value)
        {
          yield return $"\t\t{point.Item1} = {point.Item2}";
        }
      }

      if (ot.Comment.Count > 0)
      {
        yield return $"\tКомметрии:";
        foreach (var line in ot.Comment)
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
