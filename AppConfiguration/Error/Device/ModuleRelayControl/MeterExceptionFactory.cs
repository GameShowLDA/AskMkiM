using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfiguration.Error.Device.ModuleRelayControl
{
  /// <summary>
  /// Фабрика исключений для операций с измерителем модуля МКР.
  /// </summary>
  public static class MeterExceptionFactory
  {
    /// <summary>
    /// Исключение при ошибке подключения измерителя.
    /// </summary>
    /// <param name="description">Описание устройства или действия.</param>
    public static DeviceException ConnectFailed(string description) =>
        new($"Ошибка подключения измерителя {description}");

    /// <summary>
    /// Исключение при ошибке отключения измерителя.
    /// </summary>
    /// <param name="description">Описание устройства или действия.</param>
    public static DeviceException DisconnectFailed(string description) =>
        new($"Ошибка отключения измерителя {description}");
  }
}
