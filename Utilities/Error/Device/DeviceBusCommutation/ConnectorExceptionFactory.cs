using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Error.Device.DeviceBusCommutation
{
  public class ConnectorExceptionFactory
  {
    /// <summary>
    /// Исключение при ошибке подключения устройства.
    /// </summary>
    public static DeviceException ConnectFailed(string description) =>
        new($"Ошибка подключения {description}");

    /// <summary>
    /// Исключение при ошибке отключения устройства.
    /// </summary>
    public static DeviceException DisconnectFailed(string description) =>
        new($"Ошибка отключения {description}");
  }
}
