using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NewCore.Device;

namespace NewCore.Function.Keysight3466new
{

  public class KeysightCommunication
  {
    private readonly KeysightDevice _device;
    private readonly int _port = 5025;
    private TcpClient _client;
    private NetworkStream _stream;

    public KeysightCommunication(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <summary>
    /// Подключается к прибору.
    /// </summary>
    public async Task<bool> ConnectAsync()
    {
      if (_device.IP == null) throw new InvalidOperationException("IP-адрес прибора не задан.");

      try
      {
        _client = new TcpClient();
        await _client.ConnectAsync(_device.IP.ToString(), _port);
        _stream = _client.GetStream();
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
      _stream?.Close();
      _client?.Close();
      _stream = null;
      _client = null;
      _device.IsConnected = false;
    }

    /// <summary>
    /// Отправляет SCPI-команду без ожидания ответа.
    /// </summary>
    public async Task SendCommandAsync(string command)
    {
      byte[] data = Encoding.ASCII.GetBytes(command + "\n");
      await _stream.WriteAsync(data, 0, data.Length);
    }

    /// <summary>
    /// Отправляет SCPI-команду и получает ответ.
    /// </summary>
    public async Task<string> QueryAsync(string command)
    {
      await SendCommandAsync(command);

      byte[] buffer = new byte[1024];
      int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
      return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
    }
  }
}