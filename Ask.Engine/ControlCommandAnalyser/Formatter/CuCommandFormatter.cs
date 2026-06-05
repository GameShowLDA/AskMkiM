using Ask.Engine.ControlCommandAnalyser.Formatter.Base;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  public class CuCommandFormatter : CommandFormatter<CuCommandModel>
  {
    protected override IEnumerable<string> Format(CuCommandModel cu)
    {
      var header = $"{cu.CommandNumber} {cu.Mnemonic}" + (cu.IsDocument ? " Д" : "");
      foreach (var line in FormatCommandStart(cu, header))
      {
        yield return line;
      }

      yield return $"\tТип сообщения: {cu.CuType}";
      yield return "\tТекст сообщения:";

      foreach (var line in FormatMessageText(cu.MessageText))
      {
        yield return line;
      }

      foreach (var line in FormatComments(cu))
      {
        yield return line;
      }

      foreach (var line in FormatEnd())
      {
        yield return line;
      }
    }

    private static IEnumerable<string> FormatMessageText(string? messageText)
    {
      if (string.IsNullOrEmpty(messageText))
      {
        yield break;
      }

      foreach (var line in messageText.Split('\n', '\r'))
      {
        var trimmed = line.Trim();
        if (!string.IsNullOrEmpty(trimmed))
        {
          yield return $"\t\t{trimmed}";
        }
      }
    }
  }
}
