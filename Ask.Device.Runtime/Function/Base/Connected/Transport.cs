using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Base.Device;
using Ask.Device.Runtime.Function.Helpers;

namespace Ask.Device.Runtime.Function.Connected
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
    }

    /// <inheritdoc />
    public event Action IsReset;

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService userMessageService = null)
    {
      Func<IUserInteractionService, Task<(bool Connect, string Answer)>> connectDelegate;

      switch (connectionType)
      {
        case ConnectionType.IP_UDP:
          connectDelegate = new UdpTransport((DeviceWithUdpIp)_device).ConnectAsync;
          break;

        case ConnectionType.IP_TCP:
          connectDelegate = new TcpTransport((DeviceWithTcpIp)_device).ConnectAsync;
          break;

        case ConnectionType.COM:
          connectDelegate = new ComTransport((DeviceWithCOM)_device).ConnectAsync;
          break;

        case ConnectionType.USB:
          connectDelegate = new UsbTransport((DeviceWithUSB)_device).ConnectAsync;
          break;

        default:
          throw new NotSupportedException("Unsupported connection type");
      }

      var (connect, answer) = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var result = await connectDelegate(userMessageService);

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
      Func<IUserInteractionService, Task<bool>> disconnectDelegate;

      switch (connectionType)
      {
        case ConnectionType.IP_UDP:
          disconnectDelegate = new UdpTransport((DeviceWithUdpIp)_device).DisconnectAsync;
          break;
        case ConnectionType.IP_TCP:
          disconnectDelegate = new TcpTransport((DeviceWithTcpIp)_device).DisconnectAsync;
          break;
        case ConnectionType.COM:
          disconnectDelegate = new ComTransport((DeviceWithCOM)_device).DisconnectAsync;
          break;
        case ConnectionType.USB:
          disconnectDelegate = new UsbTransport((DeviceWithUSB)_device).DisconnectAsync;
          break;

        default:
          throw new NotSupportedException("Unsupported connection type");
      }

      var connect = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var result = await disconnectDelegate(userMessageService);

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
      Func<IUserInteractionService, Task<(bool Connect, string Answer)>> initDelegate;

      switch (connectionType)
      {
        case ConnectionType.IP_UDP:
          initDelegate = new UdpTransport((DeviceWithUdpIp)_device).InitializeAsync;
          break;
        case ConnectionType.IP_TCP:
          initDelegate = new TcpTransport((DeviceWithTcpIp)_device).InitializeAsync;
          break;
        case ConnectionType.COM:
          initDelegate = new ComTransport((DeviceWithCOM)_device).InitializeAsync;
          break;
        case ConnectionType.USB:
          initDelegate = new UsbTransport((DeviceWithUSB)_device).InitializeAsync;
          break;

        default:
          throw new NotSupportedException("Unsupported connection type");
      }

      var (connect, answer) = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var result = await initDelegate(userMessageService);

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
      Func<IUserInteractionService, Task<bool>> resetDelegate;

      switch (connectionType)
      {
        case ConnectionType.IP_UDP:
          resetDelegate = new UdpTransport((DeviceWithUdpIp)_device).ResetAsync;
          break;
        case ConnectionType.IP_TCP:
          resetDelegate = new TcpTransport((DeviceWithTcpIp)_device).ResetAsync;
          break;
        case ConnectionType.COM:
          resetDelegate = new ComTransport((DeviceWithCOM)_device).ResetAsync;
          break;
        case ConnectionType.USB:
          resetDelegate = new UsbTransport((DeviceWithUSB)_device).ResetAsync;
          break;
        default:
          throw new NotSupportedException("Unsupported connection type");
      }

      var connect = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        return await resetDelegate(userMessageService);
      }, userMessageService);

      IsReset?.Invoke();
     
      return connect;
    }
  }
}
