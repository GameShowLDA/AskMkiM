using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using NewCore.Device;
using System.Net;
using System.Net.Sockets;

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

    public event Action DeviceDisponce;
    public event Action IsReset;

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
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService messageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return (true, "Холостой режим");
      }

      if ((await ConnectAsync()).Connect)
      {
        string idn = await _device.DeviceProtocol.QueryAsync("*IDN?", timeout: 1000, port: _device.Port);
        if (!string.IsNullOrEmpty(idn))
        {
          return (true, string.Empty);
        }
      }

      return (false, "Нет подключения к мультиметру Keysight.");
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService messageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return (true, string.Empty);
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

      using var token = new CancellationTokenSource(2000);

      try
      {
        _device.Client = new TcpClient();
        await _device.Client.ConnectAsync(_device.IP.ToString(), _device.Port, token.Token);
        _device.Stream = _device.Client.GetStream();
        _device.IsConnected = true;
        return (true, string.Empty);
      }
      catch (OperationCanceledException)
      {
        return (false, "Превышено время подлючения к мультиметру: 2сек.");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка подключения: {ex.Message}");
        _device.IsConnected = false;
        return (false, ex.Message);
      }

    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync(IUserInteractionService messageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      await _device.DeviceProtocol.OperationLock.WaitAsync();
      try
      {
        _device.Stream?.Close();
        _device.Stream = null;

        _device.Client?.Close();
        _device.Client = null;

        _device.IsConnected = false;

        IsReset?.Invoke();

        return true;
      }
      finally
      {
        _device.DeviceProtocol.OperationLock.Release();
      }
    }

    /// <inheritdoc />
    public Task<bool> ResetAsync(IUserInteractionService messageService = null)
    {
      return Task.FromResult(true);
    }

    public string GetConnectionStatus()
    {
      var mode = "Режим: ";
      switch (_device.TypeMode)
      {
        case MultimeterTypeMode.None:
          mode += "Не задан";
          break;
        case MultimeterTypeMode.AcVoltage:
          mode += "Измерение переменного напряжения";
          break;
        case MultimeterTypeMode.DcVoltage:
          mode += "Измерение постоянного напряжения";
          break;
        case MultimeterTypeMode.Capacitance:
          mode += "Измерение ёмкости.";
          break;
        case MultimeterTypeMode.Continuity:
          mode += "Прозвонка.";
          break;
        case MultimeterTypeMode.Resistance:
          mode += "Измерение электрического сопротивления.";
          break;
      }

      return mode;
    }
  }
}
