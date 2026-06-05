using Ask.Core.Shared.Interfaces.ParserInterfaces;

namespace Ask.Engine.ControlCommandAnalyser.Formatter.Base
{
  internal class TimeFormatter
  {
    public static string FormatTime(
     IHasTime model,
     string title,
     string? indent = "\t",
     bool showNotSet = true)
    {
      return FormatTime(model.TimeSource, title, indent, showNotSet);
    }

    public static string FormatTime(
      string? time,
      string title,
      string? indent = "\t",
      bool showNotSet = true)
    {
      return SourceValueFormatter.Format(time, title, $"{title} не задано!", indent, showNotSet);
    }
  }
}
