using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mode.Metrology.MeasurementSystem
{
  /// <summary>
  /// Менеджер управления процессом измерений.
  /// </summary>
  public class MeasurementManager
  {
    /// <summary>
    /// Запускает измерение с указанным алгоритмом.
    /// </summary>
    /// <param name="measurement">Алгоритм измерения.</param>
    /// <param name="point1">Первая точка.</param>
    /// <param name="point2">Вторая точка.</param>
    /// <param name="referenceValue">Эталонное значение.</param>
    public void StartMeasurement(BaseMeasurement measurement, string point1, string point2, string referenceValue)
    {
      measurement.ExecuteMeasurement(point1, point2, referenceValue);
    }
  }
}
