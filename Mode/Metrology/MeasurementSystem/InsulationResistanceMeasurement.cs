using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mode.Metrology.MeasurementSystem
{
  /// <summary>
  /// Алгоритм измерения сопротивления изоляции.
  /// Использует ППУ.
  /// </summary>
  public class InsulationResistanceMeasurement : BaseMeasurement
  {
    /// <summary>
    /// Настраивает ППУ для измерения сопротивления изоляции.
    /// </summary>
    protected override void ConfigureMultimeter()
    {
      // TODO: Установить ППУ в режим измерения сопротивления изоляции
    }
  }
}
