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

      // Время
      if (!string.IsNullOrWhiteSpace(ot.TimeSource))
      {
        yield return $"\tВремя отключения точек: {ot.TimeSource}";
      }
      else
      {
        yield return $"\tВремя отключения точек не задано!";
      }

      foreach (var bus in ot.BusPointsDictionary)
      {
        yield return $"\tТочки, отключаемые от шины: {bus.Key}";
        foreach (var point in bus.Value)
        {
          yield return $"\t\t{point.Mnemonic} = {point.ToString()}";
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
