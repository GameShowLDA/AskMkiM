using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Enums;

namespace AppConfiguration.MeasurementError
{
  public interface IMeasurementErrorProvider
  {
    /// <summary>
    /// Возвращает числовое и процентное значение погрешности по типу команды.
    /// </summary>
    (double Numeric, double Percent) GetErrorParameters(TypeCommand type);

    /// <summary>
    /// Возвращает диапазон допустимых значений (минимум и максимум) для данной команды и ожидаемого значения.
    /// </summary>
    (double Min, double Max) GetRange(TypeCommand typeCommand, double expectedValue);
  }
}
