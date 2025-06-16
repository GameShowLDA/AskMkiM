using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Error.Device.Adapters;

namespace AppConfiguration.Error.Device
{
  /// <summary>
  /// Фабрика исключений, связанных с подключением и инициализацией устройств.
  /// </summary>
  public static class ConnectionExceptionFactory
  {
    /// <summary>
    /// Исключение при ошибке подключения устройства.
    /// </summary>
    public static DeviceException ConnectFailed(string name, int chassis, int number, string reason = null) => ConnectionExceptionAdapter.ConnectFailed(name, chassis, number, reason);

    /// <summary>
    /// Исключение при ошибке отключения устройства.
    /// </summary>
    public static DeviceException DisconnectFailed(string name, int chassis, int number, string reason = null) => ConnectionExceptionAdapter.DisconnectFailed(name, chassis, number, reason);
    /// <summary>
    /// Исключение при ошибке инициализации устройства.
    /// </summary>
    public static DeviceException InitializeFailed(string name, int chassis, int number, string reason = null) => ConnectionExceptionAdapter.InitializeFailed(name, chassis, number, reason);

    /// <summary>
    /// Исключение при ошибке сброса устройства.
    /// </summary>
    public static DeviceException ResetFailed(string name, int chassis, int number, string reason = null) => ConnectionExceptionAdapter.ResetFailed(name, chassis, number, reason);
  }
}
