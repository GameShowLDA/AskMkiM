using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Base.DeviceProtocol;
using Ask.Device.Runtime.Base.Helpers;
using Ask.Protocol.Messages.EntryPoints;
using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing;
using Ask.Device.ResponseProcessor.DeviceBusCommutation.ResponseProcessing;

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
        string? error = string.IsNullOrWhiteSpace(result.Answer) ? null : result.Answer;
        if (_device is IRelaySwitchModule module)
        {
          await ModuleRelayControlResponseProcessor.PublishConnectionResultAsync(
            module, result.Connect, error, userMessageService);
        }
        else if (_device is ISwitchingDevice switchingDevice)
        {
          await DeviceBusCommutationResponseProcessor.PublishConnectionResultAsync(
            switchingDevice, result.Connect, error, userMessageService);
        }
        else
        {
          await EquipmentMessages.PublishConnectionResultAsync(_device, result.Connect, error, userMessageService);
        }
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
        if (_device is IRelaySwitchModule module)
        {
          await ModuleRelayControlResponseProcessor.PublishDisconnectionResultAsync(
            module, result, userMessageService);
        }
        else if (_device is ISwitchingDevice switchingDevice)
        {
          await DeviceBusCommutationResponseProcessor.PublishDisconnectionResultAsync(
            switchingDevice, result, userMessageService);
        }
        else
        {
          await EquipmentMessages.PublishDisconnectionResultAsync(
            _device, result, outputService: userMessageService);
        }
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
        string? error = string.IsNullOrWhiteSpace(result.Answer) ? null : result.Answer;
        if (_device is IRelaySwitchModule module)
        {
          await ModuleRelayControlResponseProcessor.PublishInitializationResultAsync(
            module, result.Connect, error, userMessageService);
        }
        else if (_device is ISwitchingDevice switchingDevice)
        {
          await DeviceBusCommutationResponseProcessor.PublishInitializationResultAsync(
            switchingDevice, result.Connect, error, userMessageService);
        }
        else
        {
          await EquipmentMessages.PublishInitializationResultAsync(
            _device, result.Connect, error, userMessageService);
        }
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
        if (_device is IRelaySwitchModule module)
        {
          await ModuleRelayControlResponseProcessor.PublishResetResultAsync(
            module, result, userMessageService);
        }
        else if (_device is ISwitchingDevice switchingDevice)
        {
          await DeviceBusCommutationResponseProcessor.PublishResetResultAsync(
            switchingDevice, result, userMessageService);
        }
        else
        {
          await EquipmentMessages.PublishResetResultAsync(
            _device, result, outputService: userMessageService);
        }

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


