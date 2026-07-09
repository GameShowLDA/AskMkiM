using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Base.Device;
using System.Net;
using System.Net.Sockets;

namespace Ask.Device.Runtime.Function.Connected
{
  internal class TcpTransport : IConnectable
  {
    private DeviceWithTcpIp _device;
    public event Action IsReset;

    public TcpTransport(DeviceWithTcpIp device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return (true, "Холостой режим");
      }

      if ((await ConnectAsync()).Connect)
      {
        string idn = await _device.DeviceProtocol.QueryAsync(_device.ConnectedProfile.Initialize, timeout: _device.ConnectedProfile.Timeout, port: _device.ConnectedProfile.Port);
        if (!string.IsNullOrEmpty(idn))
        {
          return (true, string.Empty);
        }
      }

      return (false, $"Нет подключения к {_device.Name}");
    }

    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return (true, string.Empty);
      }

      if (_device.IPAddress == null)
      {
        if (IPAddress.TryParse(_device.ConnectionDetails, out IPAddress ip))
        {
          _device.IPAddress = ip;
        }
        else
        {
          throw new InvalidOperationException("IP-адрес прибора не задан.");
        }
      }

      using var token = new CancellationTokenSource(2000);

      try
      {
        _device.ConnectedProfile.TcpClient = new TcpClient();
        await _device.ConnectedProfile.TcpClient.ConnectAsync(_device.IPAddress.ToString(), _device.ConnectedProfile.Port, token.Token);
        _device.ConnectedProfile.Stream = _device.ConnectedProfile.TcpClient.GetStream();
        _device.ConnectionInfo.IsConnected = true;
        return (true, string.Empty);
      }
      catch (OperationCanceledException)
      {
        return (false, $"Превышено время подлючения к {_device.Name}: 2сек.");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка подключения: {ex.Message}");
        _device.ConnectionInfo.IsConnected = false;
        return (false, ex.Message);
      }
    }

    public async Task<bool> DisconnectAsync(IUserInteractionService userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      await _device.DeviceProtocol.OperationLock.WaitAsync();
      try
      {
        _device.ConnectedProfile.Stream?.Close();
        _device.ConnectedProfile.Stream = null;

        _device.ConnectedProfile.TcpClient?.Close();
        _device.ConnectedProfile.TcpClient = null;

        _device.ConnectionInfo.IsConnected = false;

        IsReset?.Invoke();

        return true;
      }
      finally
      {
        _device.DeviceProtocol.OperationLock.Release();
      }
    }

    public Task<bool> ResetAsync(IUserInteractionService userMessageService = null)
    {
      return Task.FromResult(true);
    }
  }
}
