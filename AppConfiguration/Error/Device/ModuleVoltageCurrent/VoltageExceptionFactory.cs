using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfiguration.Error.Device.ModuleVoltageCurrent
{
  /// <summary>
  /// Фабрика исключений для операций управления напряжением в модуле МИНТ.
  /// </summary>
  public static class VoltageExceptionFactory
  {
    /// <summary>
    /// Исключение при ошибке установки источника напряжения.
    /// </summary>
    public static DeviceException SetSourceFailed(string source, string reason = null) =>
        new($"Ошибка выбора источника напряжения ({source}){Format(reason)}");

    /// <summary>
    /// Исключение при ошибке установки уровня напряжения.
    /// </summary>
    public static DeviceException SetLevelFailed(string value, string reason = null) =>
        new($"Ошибка установки напряжения {value} В{Format(reason)}");

    private static string Format(string reason) =>
        string.IsNullOrWhiteSpace(reason) ? string.Empty : $": {reason}";
  }
}
