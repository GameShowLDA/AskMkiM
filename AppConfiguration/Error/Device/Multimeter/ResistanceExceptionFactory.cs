using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfiguration.Error.Device.Multimeter
{
  public static class ResistanceExceptionFactory
  {
    /// <summary>
    /// Исключение при ошибке установки режима RES.
    /// </summary>
    public static DeviceException SetModeFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки режима измерения сопротивления {name}({chassis}.{number}){Format(reason)}");

    /// <summary>
    /// Исключение при ошибке измерения режима RES.
    /// </summary>
    public static DeviceException SetMeasureFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка измеренния при режиме измерения сопротивления {name}({chassis}.{number}){Format(reason)}");

    private static string Format(string reason) => string.IsNullOrWhiteSpace(reason) ? string.Empty : $": {reason}";
  }
}          