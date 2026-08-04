using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Base.Device;
using Ask.Device.Runtime.Function.Connected;
using Ask.Protocol.Messages.EntryPoints;

namespace Ask.Device.Runtime.Function.Base.Connected
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
        await EquipmentMessages.PublishConnectionResultAsync(_device, result.Connect, string.IsNullOrWhiteSpace(result.Answer) ? null : result.Answer, userMessageService);
        return result;
      }, userMessageService, deviceTask: true);

      return (connect, answer);
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync(IUserInteractionService userMessageService = null)
    {
      var connect = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var result = await _connectionTransport.DisconnectAsync(userMessageService);
        await EquipmentMessages.PublishDisconnectionResultAsync(_device, result, outputService: userMessageService);
        return result;
      }, userMessageService, deviceTask: true);

      return connect;
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService userMessageService = null)
    {
      var (connect, answer) = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var result = await _connectionTransport.InitializeAsync(userMessageService);
        await EquipmentMessages.PublishInitializationResultAsync(_device, result.Connect, string.IsNullOrWhiteSpace(result.Answer) ? null : result.Answer, userMessageService);
        return result;
      }, userMessageService,
      deviceTask: true);

      return (connect, answer);
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync(IUserInteractionService userMessageService = null)
    {
      var connect = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var result = await _connectionTransport.ResetAsync(userMessageService);
        await EquipmentMessages.PublishResetResultAsync(_device, result, outputService: userMessageService);

        return result;
      }, userMessageService, deviceTask: true);

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
