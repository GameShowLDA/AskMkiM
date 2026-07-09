using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Base.Device;

namespace Ask.Device.Runtime.Function.Connected
{
  internal class Transport : IConnectable
  {
    private readonly IDevice _device;
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

    public event Action IsReset;

    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService userMessageService = null)
    {
      switch (connectionType)
      {
        case ConnectionType.IP_UDP:
          return await new UdpTransport((DeviceWithIP)_device).ConnectAsync(userMessageService);
        case ConnectionType.IP_TCP:
          return await new TcpTransport((DeviceWithIP)_device).ConnectAsync(userMessageService);
        case ConnectionType.COM:
          break;
        case ConnectionType.USB:
          return await new UsbTransport((DeviceWithUSB)_device).ConnectAsync(userMessageService);
      }

      throw new NotSupportedException("Unsupported connection type");
    }

    public async Task<bool> DisconnectAsync(IUserInteractionService userMessageService = null)
    {
      switch (connectionType)
      {
        case ConnectionType.IP_UDP:
          return await new UdpTransport((DeviceWithIP)_device).DisconnectAsync(userMessageService);
        case ConnectionType.IP_TCP:
          return await new TcpTransport((DeviceWithIP)_device).DisconnectAsync(userMessageService);
        case ConnectionType.COM:
          break;
        case ConnectionType.USB:
          return await new UsbTransport((DeviceWithUSB)_device).DisconnectAsync(userMessageService);
      }

      throw new NotSupportedException("Unsupported connection type");
    }
    public Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService userMessageService = null)
    {
      switch (connectionType)
      {
        case ConnectionType.IP_UDP:
          return new UdpTransport((DeviceWithIP)_device).InitializeAsync(userMessageService);
        case ConnectionType.IP_TCP:
          return new TcpTransport((DeviceWithIP)_device).InitializeAsync(userMessageService);
        case ConnectionType.COM:
          break;
        case ConnectionType.USB:
          return new UsbTransport((DeviceWithUSB)_device).InitializeAsync();
      }

      throw new NotSupportedException("Unsupported connection type");
    }

    public Task<bool> ResetAsync(IUserInteractionService userMessageService = null)
    {
      switch (connectionType)
      {
        case ConnectionType.IP_UDP:
          return new UdpTransport((DeviceWithIP)_device).ResetAsync(userMessageService);
        case ConnectionType.IP_TCP:
          return new TcpTransport((DeviceWithIP)_device).ResetAsync(userMessageService);
        case ConnectionType.COM:
          break;
        case ConnectionType.USB:
          return new UsbTransport((DeviceWithUSB)_device).ResetAsync();
      }

      throw new NotSupportedException("Unsupported connection type");
    }
  }
}
