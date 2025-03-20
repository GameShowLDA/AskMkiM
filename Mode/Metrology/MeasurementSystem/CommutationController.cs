using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mode.Metrology.MeasurementSystem
{
  /// <summary>
  /// Управление коммутацией.
  /// </summary>
  public class CommutationController
  {
    /// <summary>
    /// Настраивает коммутацию перед измерением.
    /// </summary>
    /// <param name="point1">Первая точка.</param>
    /// <param name="point2">Вторая точка.</param>
    public void Setup(string point1, string point2)
    {
      // TODO: Реализовать коммутацию точек
    }

    /// <summary>
    /// Размыкает коммутацию после измерения.
    /// </summary>
    public void Release()
    {
      // TODO: Реализовать размыкание реле
    }
  }
}
