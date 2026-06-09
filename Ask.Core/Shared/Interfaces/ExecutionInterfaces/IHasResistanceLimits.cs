using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces
{
  public interface IHasResistanceLimits
  {
    /// <summary>
    /// Единицы измерения сопротивления
    /// (например, "МОм", "кОм" и т.п.).
    /// </summary>
    string? ResistanceUnit { get; set; }

    /// <summary>
    /// Исходное представление нижней границы сопротивления
    /// (например, "100<МОм").
    /// </summary>
    string? LowerLimitResistanceSource { get; set; }

    /// <summary>
    /// Нижняя граница значения сопротивления.
    /// </summary>
    double? LowerLimitResistance { get; set; }

    /// <summary>
    /// Исходное представление верхней границы сопротивления
    /// (например, "МОм<100").
    /// </summary>
    string? HigherLimitResistanceSource { get; set; }

    /// <summary>
    /// Верхняя граница значения сопротивления.
    /// </summary>
    double? HigherLimitResistance { get; set; }
  }
}
