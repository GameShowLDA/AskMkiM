using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mode.Metrology.MeasurementSystem
{
  /// <summary>
  /// Управление пробойной устновки.
  /// </summary>
  internal class BreakdownDevice : MeasurementDevice
  {
    /// <inheritdoc />
    public override void ConfigureDevice()
    {
      // TODO: Реализовать настройку ППУ
    }

    /// <inheritdoc />
    public override double Measure()
    {
      // TODO: Реализовать измерение ППУ
      return 0.0;
    }
  }
}
