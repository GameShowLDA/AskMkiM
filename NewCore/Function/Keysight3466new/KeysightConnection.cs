using System.Net;
using System.Net.Sockets;
using NewCore.Base.Device;
using NewCore.Base.Function.FastMeter;
using NewCore.Device;
using static AppConfiguration.Execution.ExecutionConfig;

namespace NewCore.Function.Keysight3466new
{
  /// <summary>
  /// Класс для управления подключением к прибору Keysight через TCP/IP.
  /// </summary>
  public class KeysightConnection : IConnectable
  {
    /// <summary>
    /// Экземпляр устройства Keysight.
    /// </summary>
    private readonly KeysightDevice _device;

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
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync()
    {
      if (await GetIsIdleModeEnabled())
      {
        return (true, "Холостой режим");
      }

      if ((await ConnectAsync()).Connect)
      {
        string idn = await _device.DeviceProtocol.QueryAsync("*IDN?", timeout: 1000, port:_device.Port);
        if (!string.IsNullOrEmpty(idn))
        {
          return (true, string.Empty);
        }
      }

      return (false, "Нет подключения к маультиметру Keysight.");
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync()
    {
      if (await GetIsIdleModeEnabled())
      {
        return (true, "Холостой режим");
      }

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
        return (true, string.Empty);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка подключения: {ex.Message}");
        _device.IsConnected = false;
        return (false, ex.Message);
      }
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync()
    {
      if (await GetIsIdleModeEnabled())
      {
        return true;
      }
      
      _device.Stream?.Close();
      _device.Stream = null;

      _device.Client?.Close();
      _device.Client = null;

      _device.IsConnected = false;

      return true;
    }

    /// <inheritdoc />
    public Task<bool> ResetAsync()
    { 
      return Task.FromResult(true);
    }
  }
}
