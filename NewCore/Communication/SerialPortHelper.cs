using System.Diagnostics;
using System.IO.Ports;
using System.Management;
using static Utilities.LoggerUtility;

namespace NewCore.Communication
{
  /// <summary>
  /// Вспомогательный класс для управления всеми SerialPort в системе.
  /// Хранит уникальный список открытых портов и позволяет закрывать их по требованию.
  /// </summary>
  public static class SerialPortHelper
  {
    private static readonly List<SerialPort> _ports = new();

    /// <summary>
    /// Регистрирует SerialPort в списке. Если порт с таким именем уже существует — старый закрывается и удаляется.
    /// </summary>
    /// <param name="port">SerialPort для регистрации.</param>
    public static void RegisterSerialPort(SerialPort port)
    {
      if (port == null)
        return;

      lock (_ports)
      {
        var existing = _ports.FirstOrDefault(p => string.Equals(p.PortName, port.PortName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
          try
          {
            if (existing.IsOpen)
            {
              existing.Close();
              LogInformation($"Закрыт старый порт {existing.PortName} перед заменой.", isDeviceLog: true);
            }
          }
          catch (Exception ex)
          {
            LogException(ex, $"Ошибка при закрытии порта {existing.PortName}", isDeviceLog: true);
          }

          _ports.Remove(existing);
          LogInformation($"Старый порт {existing.PortName} удалён из списка.", isDeviceLog: true);
        }

        _ports.Add(port);
        LogInformation($"Порт {port.PortName} зарегистрирован.", isDeviceLog: true);
      }
    }

    /// <summary>
    /// Закрывает все зарегистрированные SerialPort и очищает список.
    /// </summary>
    public static void CloseAllRegisteredSerialPorts()
    {
      lock (_ports)
      {
        foreach (var port in _ports)
        {
          try
          {
            if (port != null && port.IsOpen)
            {
              LogInformation($"[{port.PortName}] Вызываю Close()", isDeviceLog: true);
              port.Close();
              LogInformation($"Порт {port.PortName} закрыт.", isDeviceLog: true);

            }
          }
          catch (Exception ex)
          {
            LogException(ex, $"Ошибка при закрытии порта {port.PortName}", isDeviceLog: true);
          }
        }

        _ports.Clear();
        LogInformation("Все зарегистрированные порты удалены из списка.", isDeviceLog: true);
      }
    }
  }
}
