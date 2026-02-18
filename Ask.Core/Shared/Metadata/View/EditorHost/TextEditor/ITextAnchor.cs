using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Metadata.View.EditorHost.TextEditor
{
  /// <summary>
  /// Отслеживаемый диапазон текста (tracking range).
  ///
  /// Представляет участок документа, который автоматически перемещается
  /// при редактировании текста. Используется для:
  ///  • защищённых областей
  ///  • диагностик
  ///  • точек привязки логики (breakpoints, маркеры, блоки)
  ///
  /// В отличие от обычного сегмента, координаты якоря изменяются при вставке
  /// или удалении текста перед ним.
  /// 
  /// Контракт:
  ///  • Offset — текущая позиция начала диапазона (0-based)
  ///  • Length — текущая длина диапазона
  /// </summary>
  public interface ITextAnchor
  {
    /// <summary>
    /// Текущий offset начала диапазона (0-based).
    /// Значение может изменяться при редактировании документа.
    /// </summary>
    int Offset { get; }

    /// <summary>
    /// Текущая длина диапазона.
    /// </summary>
    int Length { get; }
  }
}
