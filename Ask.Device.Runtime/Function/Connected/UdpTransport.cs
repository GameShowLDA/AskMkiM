using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Base.Device;

namespace Ask.Device.Runtime.Function.Connected
{
  internal class UdpTransport : IConnectable
  {
    private DeviceWithIP _device;
    public event Action IsReset;

    public UdpTransport(DeviceWithIP device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return (true, String.Empty);
      }

      string result = await _device.DeviceProtocol.QueryAsync(_device.ConnectedProfile.Initialize, timeout: _device.ConnectedProfile.Timeout);
      if (string.IsNullOrEmpty(result))
      {
        IsReset?.Invoke();
        return (false, $"Нет ответа от устройства {_device.Name}({_device.Number})");
      }

      var initializationResult = _device.InitializationValidationDelegate(result, _device);
      if (initializationResult.Success)
      {
        return (true, string.Empty);
      }

      IsReset?.Invoke();
      return (false, initializationResult.Message);
    }

    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService userMessageService = null)
    {
      return await InitializeAsync();
    }

    public async Task<bool> DisconnectAsync(IUserInteractionService userMessageService = null)
    {
      return await ResetAsync();
    }

    public async Task<bool> ResetAsync(IUserInteractionService userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        IsReset?.Invoke();
        return true;
      }

      string result = await _device.DeviceProtocol.QueryAsync(_device.ConnectedProfile.Initialize, timeout: _device.ConnectedProfile.Timeout);
      var resetResult = _device.ResetValidationDelegate(result, _device);
      IsReset?.Invoke();

      if (resetResult)
      {
        return true;
      }

      return false;
    }

  }
}
