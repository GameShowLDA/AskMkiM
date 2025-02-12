using ICSharpCode.AvalonEdit.Document;
using System.Windows.Media;

namespace UI.Components.SearchControls
{
  public class TextMarker : TextSegment
  {
    public Color BackgroundColor { get; set; }
    public Color? ForegroundColor { get; set; }

    public TextMarker(int startOffset, int length)
    {
      StartOffset = startOffset;
      Length = length;
    }
  }
}
