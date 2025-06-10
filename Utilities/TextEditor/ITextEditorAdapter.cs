using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.TextEditor
{
  /// <summary>
  /// Универсальный фасад для взаимодействия с редактором текста.
  /// </summary>
  public interface ITextEditorAdapter
  {
    /// <summary>
    /// Устанавливает текст и диапазоны подсветки.
    /// </summary>
    /// <param name="text">Текст для редактора.</param>
    /// <param name="highlights">Список диапазонов подсветки.</param>
    void SetTextAndHighlighting(string text, List<HighlightRange> highlights);
  }
}
