using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Communication.Common.Threading;
using Ask.Device.Communication.Usb.Discovery;
using Ask.Device.Runtime.Base.DeviceProtocol;

namespace Ask.Device.Runtime.Base.Connected
{
  internal class UsbTransport : IConnectable
  {
    private DeviceWithUSB _device;

    public UsbTransport(DeviceWithUSB device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    public event Action IsReset;

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return (true, string.Empty);
      }

      string pattern = GetUsbSearchPattern();
      if (!UsbDeviceLocator.TryFindByName(pattern, out var descriptor))
      {
        _device.ConnectionInfo.IsConnected = false;
        _device.ConnectedProfile.LastResolvedDevicePath = string.Empty;
        return (false, $"USB-устройство {_device.Name} не найдено по шаблону \"{pattern}\".");
      }

      _device.ConnectionInfo.IsConnected = true;
      _device.ConnectedProfile.LastResolvedDevicePath = descriptor.DeviceId;
      return (true, descriptor.Name);
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync(IUserInteractionService userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      using (await _device.DeviceProtocol.OperationLock.LockAsync())
      {
        _device.ConnectionInfo.IsConnected = false;

        if (_device is IMultimeter multimeter)
        {
          multimeter.TypeMode = MultimeterTypeMode.None;
        }

        IsReset?.Invoke();
        return true;
      }
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return (true, "Холостой режим");
      }

      var connection = await ConnectAsync(userMessageService);
      if (!connection.Connect)
      {
        return connection;
      }

      string idn = await _device.DeviceProtocol.QueryAsync(_device.ConnectedProfile.Initialize, timeout: _device.ConnectedProfile.Timeout);
      return string.IsNullOrWhiteSpace(idn)
        ? (false, $"Нет ответа на команду {_device.ConnectedProfile.Initialize} от {_device.Name}")
        : (true, idn.Trim());
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync(IUserInteractionService userMessageService = null)
    {
      if (_device is IMultimeter multimeter)
      {
        multimeter.TypeMode = MultimeterTypeMode.None;
      }

      IsReset?.Invoke();
      return true;
    }

    private string GetUsbSearchPattern()
    {
      return string.IsNullOrWhiteSpace(_device.ConnectionDetails)
        ? _device.Name
        : _device.ConnectionDetails;
    }
  }
}
