using Ask.Engine.ControlCommandAnalyser.Formatter.Base;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  internal class OcCommandFormatter : CommandFormatter<OcCommandModel>
  {
    protected override IEnumerable<string> Format(OcCommandModel oc)
    {
      foreach (var line in FormatCommandStart(oc))
      {
        yield return line;
      }

      foreach (var line in FormatComments(oc))
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
