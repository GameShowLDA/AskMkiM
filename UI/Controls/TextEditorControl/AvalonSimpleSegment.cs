using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Controls.TextEditorControl
{
  /// <summary>
  /// Неподвижный (snapshot) сегмент текста для AvalonEdit.
  ///
  /// Представляет диапазон, вычисленный в конкретный момент времени
  /// и не отслеживающий изменения документа. Используется как
  /// lightweight-замена внутреннего SimpleSegment библиотеки AvalonEdit.
  ///
  /// В отличие от <see cref="AvalonAnchorSegment"/>:
  ///  • не использует anchor’ы
  ///  • не смещается при редактировании текста
  ///  • применяется для временных операций (рендеринг, поиск, выделение)
  /// </summary>
  internal sealed class AvalonSimpleSegment : ISegment
  {
    /// <summary>
    /// Начальная позиция диапазона (0-based).
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// Длина диапазона.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Конечная позиция диапазона (Offset + Length).
    /// </summary>
    public int EndOffset => Offset + Length;

    /// <summary>
    /// Создаёт статический сегмент текста.
    /// </summary>
    public AvalonSimpleSegment(int offset, int length)
    {
      Offset = offset;
      Length = length;
    }
  }
}
