using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBaseConfiguration
{
  /// <summary>
  /// Унифицированный интерфейс горячей клавиши.
  /// </summary>
  public interface IHotkeyBinding
  {
    string ActionName { get; }
    string KeyCombination { get; }
    bool IsEnabled { get; }
    HotkeyScope Scope { get; }
    string? Description { get; }
  }
}
