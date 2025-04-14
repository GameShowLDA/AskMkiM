using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Error.Device.ModuleVoltageCurrent
{
  /// <summary>
  /// Фабрика исключений, связанных с подключением и отключением шин к выходам модуля МИНТ.
  /// </summary>
  public class BusExceptionFactory
  {
    /// <summary>
    /// Исключение при ошибке подключения к положительному выходу.
    /// </summary>
    public static DeviceException ConnectPositiveFailed(string bus) =>
        new($"Ошибка подключения шины [{bus}] к положительному выходу (+)");

    /// <summary>
    /// Исключение при ошибке подключения к отрицательному выходу.
    /// </summary>
    public static DeviceException ConnectNegativeFailed(string bus) =>
        new($"Ошибка подключения шины [{bus}] к отрицательному выходу (-)");

    /// <summary>
    /// Исключение при ошибке отключения от положительного выхода.
    /// </summary>
    public static DeviceException DisconnectPositiveFailed(string bus) =>
        new($"Ошибка отключения шины [{bus}] от положительного выхода (+)");

    /// <summary>
    /// Исключение при ошибке отключения от отрицательного выхода.
    /// </summary>
    public static DeviceException DisconnectNegativeFailed(string bus) =>
        new($"Ошибка отключения шины [{bus}] от отрицательного выхода (-)");
  }
}
