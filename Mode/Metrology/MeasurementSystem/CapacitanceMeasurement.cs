using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mode.Metrology.MeasurementSystem
{
  /// <summary>
  /// Алгоритм измерения ёмкости.
  /// Использует мультиметр.
  /// </summary>
  public class CapacitanceMeasurement : BaseMeasurement
  {
    /// <summary>
    /// Настраивает мультиметр для измерения ёмкости.
    /// </summary>
    protected override void ConfigureMultimeter()
    {
      // TODO: Установить мультиметр в режим измерения ёмкости
    }
  }
}
