using System.Windows.Media;
using Utilities.TextEditor;

namespace ControlCommandAnalyser.Model
{
  public class ColoredLine
  {
    public string Text { get; set; }
    public Color? ColorOverride { get; set; }
    public HighlightTarget? Target { get; set; }
  }
}
