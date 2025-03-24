using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mode.Models;
using static NewCore.Enum.MetrologyEnum;

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
    /// <param name="mode">Метрологический режим.</param>
    /// <param name="point1">Первая точка.</param>
    /// <param name="point2">Вторая точка.</param>
    /// <param name="referenceValue">Эталонное значение.</param>
    public void StartMeasurement(BaseMeasurement measurement, MetrologicalModeRole mode,  PointModel point1, PointModel point2, double referenceValue)
    {
      measurement.ExecuteMeasurement(mode, point1, point2, referenceValue);
    }
  }
}
