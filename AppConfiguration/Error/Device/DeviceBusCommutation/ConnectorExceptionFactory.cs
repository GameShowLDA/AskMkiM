using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfiguration.Error.Device.DeviceBusCommutation
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

    /// <summary>
    /// Исключение при ошибке подключения устройства.
    /// </summary>
    public static DeviceException ConnectBreakdownFailed(string name, int chassis, int number) =>
        new($"Ошибка подключения ППУ на {name}({chassis}.{number})");

    /// <summary>
    /// Исключение при ошибке отключения устройства.
    /// </summary>
    public static DeviceException DisconnectBreakdownFailed(string name, int chassis, int number) =>
        new($"Ошибка отключения ППУ на {name}({chassis}.{number})");

    /// <summary>
    /// Исключение при ошибке подключения устройства.
    /// </summary>
    public static DeviceException ConnectMultiMeterFailed(string name, int chassis, int number) =>
        new($"Ошибка подключения Мультиметра на {name}({chassis}.{number})");

    /// <summary>
    /// Исключение при ошибке подключения устройства.
    /// </summary>
    public static DeviceException DisconnectMultiMeterFailed(string name, int chassis, int number) =>
        new($"Ошибка подключения Мультиметра на {name}({chassis}.{number})");
  }
}
