using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mode.Metrology.MeasurementSystem
{
  /// <summary>
  /// Управление мультиметром.
  /// </summary>
  public class MultimeterDevice : MeasurementDevice
  {
    /// <inheritdoc />
    public override void ConfigureDevice()
    {
      // TODO: Реализовать настройку мультиметра
    }

    /// <inheritdoc />
    public override double Measure()
    {
      // TODO: Реализовать измерение мультиметром
      return 0.0;
    }
  }
}
