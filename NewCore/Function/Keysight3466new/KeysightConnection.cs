using System.Net;
using System.Net.Sockets;
using NewCore.Base.Function.FastMeter;
using NewCore.Device;

namespace NewCore.Function.Keysight3466new
{
  /// <summary>
  /// Класс для управления подключением к прибору Keysight через TCP/IP.
  /// </summary>
  public class KeysightConnection : IConnection
  {
    /// <summary>
    /// Экземпляр устройства Keysight.
    /// </summary>
    private readonly KeysightDevice _device;

    /// <summary>
    /// Менеджер коммуникации для обмена SCPI-командами.
    /// </summary>
    private readonly ICommunication _communication;

    /// <summary>
    /// Возвращает состояние подключения к прибору.
    /// </summary>
    public bool IsConnected => _device.IsConnected;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="KeysightConnection"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор <c>null</c>.</exception>
    public KeysightConnection(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _communication = device.CommunicationManager;
    }

    /// <summary>
    /// Инициализирует подключение к прибору.
    /// Проверяет доступность устройства с помощью команды "*IDN?".
    /// </summary>
    /// <returns>Возвращает <c>true</c>, если подключение успешно и устройство отвечает, иначе <c>false</c>.</returns>
    public async Task<bool> InitializeAsync()
    {
      if (await ConnectAsync())
      {
        string idn = await _communication.QueryAsync("*IDN?");
        if (!string.IsNullOrEmpty(idn))
        {
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// Подключается к прибору через TCP/IP.
    /// </summary>
    /// <returns>Возвращает <c>true</c>, если подключение успешно, иначе <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если IP-адрес прибора не задан.</exception>
    public async Task<bool> ConnectAsync()
    {
      if (_device.IP == null)
      {
        if (IPAddress.TryParse(_device.ConnectionDetails, out IPAddress ip))
        {
          _device.IP = ip;
        }
        else
        {
          throw new InvalidOperationException("IP-адрес прибора не задан.");
        }
      }

      try
      {
        _device.Client = new TcpClient();
        await _device.Client.ConnectAsync(_device.IP.ToString(), _device.Port);
        _device.Stream = _device.Client.GetStream();
        _device.IsConnected = true;
        return true;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка подключения: {ex.Message}");
        _device.IsConnected = false;
        return false;
      }
    }

    /// <summary>
    /// Отключается от прибора.
    /// Закрывает поток данных и TCP-соединение.
    /// </summary>
    public void Disconnect()
    {
      _device.Stream?.Close();
      _device.Stream = null;

      _device.Client?.Close();
      _device.Client = null;

      _device.IsConnected = false;
    }
  }
}
