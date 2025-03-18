using System.Net;
using System.Net.Sockets;
using NewCore.Base.Function.FastMeter;
using NewCore.Device;

namespace NewCore.Function.Keysight3466new
{
  public class KeysightConnection : IConnection
  {
    private readonly KeysightDevice _device;
    private readonly ICommunication _communication;

    public bool IsConnected => _device.IsConnected;

    public KeysightConnection(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _communication = device.CommunicationManager;
    }

    /// <summary>
    /// Инициализация устройства.
    /// </summary>
    /// <returns></returns>
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
    /// Подключается к прибору.
    /// </summary>
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
