using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Error.Device
{
  /// <summary>
  /// Базовое исключение, возникающее при ошибках, связанных с оборудованием.
  /// Используется для всех режимов и компонентов устройств (IR, DCW, PR и др.).
  /// </summary>
  public class DeviceException : Exception
  {
    /// <summary>
    /// Создаёт новое исключение устройства с заданным сообщением.
    /// </summary>
    /// <param name="message">Описание ошибки.</param>
    public DeviceException(string message) : base(message) { }

    /// <summary>
    /// Создаёт новое исключение устройства с сообщением и вложенным исключением.
    /// </summary>
    /// <param name="message">Описание ошибки.</param>
    /// <param name="innerException">Внутреннее исключение.</param>
    public DeviceException(string message, Exception innerException) : base(message, innerException) { }
  }
}
