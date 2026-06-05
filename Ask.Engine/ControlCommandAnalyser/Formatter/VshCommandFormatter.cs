using Ask.Engine.ControlCommandAnalyser.Formatter.Base;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  public class VshCommandFormatter : CommandFormatter<VshCommandModel>
  {
    protected override IEnumerable<string> Format(VshCommandModel vsh)
    {
      foreach (var line in FormatCommandStart(vsh))
      {
        yield return line;
      }

      foreach (var line in SchemeFormatter.FormatCommutationRackStructure(vsh))
      {
        yield return line;
      }

      foreach (var line in FormatComments(vsh))
      {
        yield return line;
      }

      foreach (var line in FormatEnd())
      {
        yield return line;
      }
    }
  }
}
