using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Base.DeviceProtocol;
using Ask.Device.Runtime.Base.Helpers;

namespace Ask.Device.Runtime.Base.Connected
{
  /// <summary>
  /// Универсальный транспорт для подключения и обмена данными с устройством.
  /// </summary>
  internal class Transport : IConnectable
  {
    /// <summary>
    /// Устройство, с которым выполняется обмен данными.
    /// </summary>
    private readonly IDevice _device;
    private readonly IConnectable _connectionTransport;

    /// <summary>
    /// Тип подключения, используемый для взаимодействия с устройством.
    /// </summary>
    private ConnectionType connectionType;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="KeysightConnection"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор <c>null</c>.</exception>
    public Transport(IDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      connectionType = _device.ConnectionInfo.ConnectionType;
      _connectionTransport = CreateConnectionTransport();
      _connectionTransport.IsReset += () => IsReset?.Invoke();
    }

    /// <inheritdoc />
    public event Action IsReset;

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService userMessageService = null)
    {
      var (connect, answer) = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var result = await _connectionTransport.ConnectAsync(userMessageService);

        if (!result.Connect || DeviceDisplayConfig.GetExecutionParametersVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync((IAttachableDevice)_device, $"Подключение {_device.Name}", string.IsNullOrWhiteSpace(result.Answer) ? string.Empty : result.Answer, result.Connect, 1, userMessageService);
        }

        return result;
      }, userMessageService);

      return (connect, answer);
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync(IUserInteractionService userMessageService = null)
    {
      var connect = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var result = await _connectionTransport.DisconnectAsync(userMessageService);

        if (!result || DeviceDisplayConfig.GetExecutionParametersVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync((IAttachableDevice)_device, $"Отключение {_device.Name}", result ? "Соединение разорвано" : "Ошибка отключения", result, 1, userMessageService);
        }

        return result;
      }, userMessageService);

      return connect;
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService userMessageService = null)
    {
      var (connect, answer) = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var result = await _connectionTransport.InitializeAsync(userMessageService);

        if (!result.Connect || DeviceDisplayConfig.GetExecutionParametersVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync((IAttachableDevice)_device, $"Инициализация {_device.Name}", string.IsNullOrWhiteSpace(result.Answer) ? string.Empty : result.Answer, result.Connect, 1, userMessageService);
        }

        return result;
      }, userMessageService);

      return (connect, answer);
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync(IUserInteractionService userMessageService = null)
    {
      var connect = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        return await _connectionTransport.ResetAsync(userMessageService);
      }, userMessageService);
      return connect;
    }

    private IConnectable CreateConnectionTransport() => connectionType switch
    {
      ConnectionType.IP_UDP => new UdpTransport((DeviceWithUdpIp)_device),
      ConnectionType.IP_TCP => new TcpTransport((DeviceWithTcpIp)_device),
      ConnectionType.COM => new ComTransport((DeviceWithCOM)_device),
      ConnectionType.USB => new UsbTransport((DeviceWithUSB)_device),
      _ => throw new NotSupportedException($"Unsupported connection type: {connectionType}"),
    };
  }
}
