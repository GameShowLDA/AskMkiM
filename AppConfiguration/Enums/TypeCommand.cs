using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Base;

namespace AppConfiguration.Enums
{
  /// <summary>
  /// Перечисление, представляющее различные типы команд в системе.
  /// </summary>
  public enum TypeCommand
  {
    [CommandInfo("КС", "Ом", 1.0, 0.5)]
    /// <summary>
    /// Тип команды KC.
    /// </summary>
    KC,

    [CommandInfo("ПР", "Ом", 1.0, 0.1)]
    /// <summary>
    /// Тип команды PR.
    /// </summary>
    PR,

    [CommandInfo("СИ", "МОм", 1.0, 0.1)]
    /// <summary>
    /// Тип команды CI.
    /// </summary>
    CI,

    [CommandInfo("ИЕ", "нФ", 1.0, 0.1)]
    /// <summary>
    /// Тип команды IE.
    /// </summary>
    IE,
  }
}
