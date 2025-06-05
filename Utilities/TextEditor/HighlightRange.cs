using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Utilities.TextEditor
{
  /// <summary>
  /// Представляет диапазон текста, который должен быть подсвечен в редакторе.
  /// </summary>
  public class HighlightRange
  {
    /// <summary>
    /// Номер строки, где находится подсветка (относительно блока).
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Начальная позиция подсветки в строке.
    /// </summary>
    public int Start { get; set; }

    /// <summary>
    /// Длина подсвечиваемого участка.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// Тип цели подсветки (например, номер, мнемоника, параметр).
    /// </summary>
    public HighlightTarget Target { get; set; }

    /// <summary>
    /// Явно заданный цвет для этого диапазона.
    /// </summary>
    public Color? ColorOverride { get; set; }

    public HighlightRange(int line, int start, int length, HighlightTarget target)
    {
      Line = line;
      Start = start;
      Length = length;
      Target = target;
    }
  }

  /// <summary>
  /// Цели подсветки (для HighlightRange).
  /// </summary>
  public enum HighlightTarget
  {
    CommandNumber,
    Mnemonic,
    Parameter
  }
}
