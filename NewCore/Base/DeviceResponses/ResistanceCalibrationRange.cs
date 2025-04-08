using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NewCore.Enum.DeviceEnum;

namespace NewCore.Base.DeviceResponses
{
  /// <summary>
  /// Диапазон сопротивления с калибровочными коэффициентами
  /// </summary>
  public class ResistanceCalibrationRange
  {
    /// <summary>
    /// Минимальное значение сопротивления в диапазоне (в Омах).
    /// </summary>
    public int ResistanceMin { get; set; }

    /// <summary>
    /// Максимальное значение сопротивления в диапазоне (в Омах).
    /// </summary>
    public int ResistanceMax { get; set; }

    /// <summary>
    /// Целая часть рекомендуемого тока (в миллиамперах).
    /// </summary>
    public int IntegerCurrent { get; set; }

    /// <summary>
    /// Дробная часть рекомендуемого тока (в миллиамперах).
    /// </summary>
    public int DecimalCurrent { get; set; }

    /// <summary>
    /// Целая часть рекомендуемого тока (в миллиамперах).
    /// </summary>
    public int IntegerCurrentFake { get; set; }

    /// <summary>
    /// Дробная часть рекомендуемого тока (в миллиамперах).
    /// </summary>
    public int DecimalCurrentFake { get; set; }

    /// <summary>
    /// Выдаваемое напряжение на диапазоне.
    /// </summary>
    public VoltageSources Voltage { get; set; }
  }
}