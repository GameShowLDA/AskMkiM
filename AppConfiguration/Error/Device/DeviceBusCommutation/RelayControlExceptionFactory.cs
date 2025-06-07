using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfiguration.Error.Device.DeviceBusCommutation
{
  /// <summary>
  /// Фабрика исключений для ошибок подключения и отключения реле устройства коммутации шин.
  /// </summary>
  public static class RelayControlExceptionFactory
  {
    /// <summary>
    /// Исключение при ошибке подключения реле.
    /// </summary>
    public static DeviceException ConnectFailed(int number) =>
        new($"Ошибка подключения реле №{number}");

    /// <summary>
    /// Исключение при ошибке отключения реле.
    /// </summary>
    public static DeviceException DisconnectFailed(int number) =>
        new($"Ошибка отключения реле №{number}");
  }
}
