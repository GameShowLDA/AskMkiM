using ICSharpCode.AvalonEdit.Document;
using System.Windows.Media;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Класс, представляющий маркер текста в документе.
  /// </summary>
  /// <remarks>
  /// Этот класс используется для хранения информации о маркере, который применяется к определенному сегменту текста.
  /// Он включает в себя цвет фона и цвета текста для выделенного сегмента, а также информацию о его позиции в документе.
  /// </remarks>
  public class TextMarker : TextSegment
  {
    /// <summary>
    /// Цвет фона для маркера.
    /// </summary>
    public Color BackgroundColor { get; set; }

    /// <summary>
    /// Цвет текста для маркера. Может быть null, если не задан.
    /// </summary>
    public Color? ForegroundColor { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="TextMarker"/> с заданными параметрами.
    /// </summary>
    /// <param name="startOffset">Смещение начала маркера в тексте.</param>
    /// <param name="length">Длина сегмента текста, на который накладывается маркер.</param>
    public TextMarker(int startOffset, int length)
    {
      StartOffset = startOffset;
      Length = length;
    }
  }
}
