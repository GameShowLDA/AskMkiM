using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mode.Metrology.MeasurementSystem
{
  /// <summary>
  /// Абстрактный класс для всех измерительных приборов.
  /// Определяет общий интерфейс работы с оборудованием.
  /// </summary>
  public abstract class MeasurementDevice
  {
    /// <summary>
    /// Устанавливает режим измерения на приборе.
    /// </summary>
    public abstract void ConfigureDevice();

    /// <summary>
    /// Выполняет измерение.
    /// </summary>
    /// <returns>Результат измерения.</returns>
    public abstract double Measure();
  }
}
