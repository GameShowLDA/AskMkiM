using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mode.Metrology.MeasurementSystem
{
  /// <summary>
  /// Алгоритм измерения сопротивления.
  /// Использует мультиметр.
  /// </summary>
  public class ResistanceMeasurement : BaseMeasurement
  {
    /// <summary>
    /// Настраивает мультиметр для измерения сопротивления.
    /// </summary>
    protected override void ConfigureMultimeter()
    {
      // TODO: Установить мультиметр в режим измерения сопротивления
    }
  }
}
