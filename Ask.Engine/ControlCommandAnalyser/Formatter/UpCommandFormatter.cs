using Ask.Engine.ControlCommandAnalyser.Formatter.Base;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  public class UpCommandFormatter : CommandFormatter<UpCommandModel>
  {
    protected override IEnumerable<string> Format(UpCommandModel up)
    {
      foreach (var line in FormatCommandStart(up, $"{up.CommandNumber} {up.Mnemonic} {up.TargetLabel}"))
      {
        yield return line;
      }

      yield return !string.IsNullOrWhiteSpace(up.TargetLabel)
        ? $"\tПереход к команде {up.TargetLabel}"
        : "\tПереходная метка не указана!";

      foreach (var line in FormatComments(up))
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
