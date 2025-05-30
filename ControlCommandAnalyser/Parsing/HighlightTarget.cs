using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Parsing
{
  /// <summary>
  /// Указывает, какой тип элемента подлежит подсветке.
  /// </summary>
  public enum HighlightTarget
  {
    CommandNumber,
    Mnemonic,
    Parameter // ⬅️ новое значение
  }
}
