using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Parsing
{
  /// <summary>
  /// Представляет диапазон текста, подлежащий подсветке.
  /// </summary>
  public class HighlightRange
  {
    public int Line { get; set; }           // Номер строки в файле
    public int Start { get; set; }          // Смещение в строке
    public int Length { get; set; }         // Длина подсвечиваемого участка
    public HighlightTarget Target { get; set; } // Тип подсветки

    public HighlightRange(int line, int start, int length, HighlightTarget target)
    {
      Line = line;
      Start = start;
      Length = length;
      Target = target;
    }
  }
}
