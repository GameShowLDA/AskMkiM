using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfiguration.Error.Device.ModuleVoltageCurrent
{
  /// <summary>
  /// Фабрика исключений для операций установки и ограничения тока в модуле МИНТ.
  /// </summary>
  public static class CurrentExceptionFactory
  {
    /// <summary>
    /// Исключение при ошибке установки уровня тока.
    /// </summary>
    public static DeviceException SetLevelFailed(string value, string reason = null) =>
        new($"Ошибка установки тока {value} мА{Format(reason)}");

    /// <summary>
    /// Исключение при ошибке ограничения выходного тока.
    /// </summary>
    public static DeviceException LimitFailed(int value, string reason = null) =>
        new($"Ошибка ограничения тока {value} мА{Format(reason)}");

    private static string Format(string reason) =>
        string.IsNullOrWhiteSpace(reason) ? string.Empty : $": {reason}";
  }
}
