using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Emulator;
using Ask.Device.Runtime.Base.DeviceProtocol;
using System.Net;
using System.Net.Sockets;

namespace Ask.Device.Runtime.Base.Connected
{
  /// <summary>
  /// Реализует подключение и обмен данными с устройством по протоколу TCP/IP.
  /// </summary>
  internal class TcpTransport : IConnectable
  {
    private DeviceWithTcpIp _device;

    /// <summary>
    /// Возникает после выполнения сброса устройства.
    /// </summary>
    public event Action IsReset;

    /// <summary>
    /// Инициализирует транспорт TCP/IP для указанного устройства.
    /// </summary>
    /// <param name="device">Устройство, работающее по протоколу TCP/IP.</param>
    public TcpTransport(DeviceWithTcpIp device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService userMessageService = null)
    {
      if (_device is IMultimeter multimeter)
      {
        if (!ExecutionConfig.GetIsIdleModeEnabled() && !(await ConnectAsync()).Connect)
        {
          return (false, $"Нет связи с {_device.Name}");
        }

        string idleResponse = $"ASK,{_device.Name},0,IDLE";
        string answer = await DeviceProtocolEmulator.QueryMultimeterAsync(
          multimeter,
          _device.ConnectedProfile.Initialize,
          idleResponse,
          timeout: _device.ConnectedProfile.Timeout,
          port: _device.ConnectedProfile.Port);
        return string.IsNullOrWhiteSpace(answer)
          ? (false, $"Нет ответа на команду {_device.ConnectedProfile.Initialize} от {_device.Name}")
          : (true, answer.Trim());
      }

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return IdleHardwareErrorSimulator.ShouldSimulateHardwareError()
          ? (false, IdleHardwareErrorSimulator.ErrorMessage)
          : (true, "Холостой режим");
      }

      if ((await ConnectAsync()).Connect)
      {
        string idn = await _device.DeviceProtocol.QueryAsync(_device.ConnectedProfile.Initialize, timeout: _device.ConnectedProfile.Timeout, port: _device.ConnectedProfile.Port);
        if (!string.IsNullOrEmpty(idn))
        {
          return (true, string.Empty);
        }
      }

      return (false, $"Нет связи с {_device.Name}");
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return IdleHardwareErrorSimulator.ShouldSimulateHardwareError()
          ? (false, IdleHardwareErrorSimulator.ErrorMessage)
          : (true, string.Empty);
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

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync(IUserInteractionService userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return !IdleHardwareErrorSimulator.ShouldSimulateHardwareError();
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

    /// <inheritdoc />
    public Task<bool> ResetAsync(IUserInteractionService userMessageService = null)
    {
      return Task.FromResult(true);
    }
  }
}

