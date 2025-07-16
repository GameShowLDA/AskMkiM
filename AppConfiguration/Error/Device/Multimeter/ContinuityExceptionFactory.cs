using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfiguration.Error.Device.Multimeter
{
  public static class ContinuityExceptionFactory
  {
    /// <summary>
    /// Исключение при ошибке установки режима  Continuity.
    /// </summary>
    public static DeviceException SetModeFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки режима прозвонки {name}({chassis}.{number}){Format(reason)}");

    /// <summary>
    /// Исключение при ошибке измерения режима  Continuity.
    /// </summary>
    public static DeviceException SetContinuityFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка проверки при режиме прозвонки {name}({chassis}.{number}){Format(reason)}");

    private static string Format(string reason) => string.IsNullOrWhiteSpace(reason) ? string.Empty : $": {reason}";
  }
}
