using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfiguration.Error.Device.ModuleRelayControl
{
  /// <summary>
  /// Фабрика исключений, связанных с подключением и отключением шин.
  /// </summary>
  public static class BusExceptionFactory
  {
    /// <summary>
    /// Исключение при ошибке подключения шины.
    /// </summary>
    /// <param name="description">Описание шины (например, "низковольтной шины [X1]").</param>
    public static DeviceException ConnectFailed(string description) =>
        new($"Ошибка подключения {description}");

    /// <summary>
    /// Исключение при ошибке подключения шины.
    /// </summary>
    /// <param name="description">Описание шины (например, "низковольтной шины [X1]").</param>
    public static DeviceException ConnectFailed(string bus, string name, int numberChassis, int number) =>
        new($"Ошибка подключения шины {bus} {name}({numberChassis}.{number})");

    /// <summary>
    /// Исключение при ошибке отключения шины.
    /// </summary>
    /// <param name="description">Описание шины.</param>
    public static DeviceException DisconnectFailed(string description) =>
        new($"Ошибка отключения {description}");

    /// <summary>
    /// Исключение при ошибке подключения шины.
    /// </summary>
    /// <param name="description">Описание шины (например, "низковольтной шины [X1]").</param>
    public static DeviceException DisconnectFailed(string bus, string name, int numberChassis, int number) =>
        new($"Ошибка отключения шины {bus} {name}({numberChassis}.{number})");
  }
}
