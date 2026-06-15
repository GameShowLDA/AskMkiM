using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Communication.Common.Threading;
using Ask.Device.Communication.Usb.Discovery;
using Ask.Device.Runtime.Device;

namespace Ask.Device.Runtime.Function.B7783
{
  public class StateManager : IConnectable
  {
    private readonly MultimeterB7783 _device;

    public StateManager(MultimeterB7783 device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    public event Action IsReset;

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

      string idn = await _device.DeviceProtocol.QueryAsync("*IDN?", timeout: 1000);
      return string.IsNullOrWhiteSpace(idn)
        ? (false, "Нет ответа на команду *IDN? от мультиметра В7-78/3.")
        : (true, idn.Trim());
    }

    public Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return Task.FromResult((true, string.Empty));
      }

      string pattern = GetUsbSearchPattern();
      if (!UsbDeviceLocator.TryFindByName(pattern, out var descriptor))
      {
        _device.IsConnected = false;
        _device.LastResolvedDevicePath = string.Empty;
        return Task.FromResult((false, $"USB-устройство В7-78/3 не найдено по шаблону \"{pattern}\"."));
      }

      _device.IsConnected = true;
      _device.LastResolvedDevicePath = descriptor.DeviceId;
      return Task.FromResult((true, descriptor.Name));
    }

    public async Task<bool> DisconnectAsync(IUserInteractionService userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      using (await _device.DeviceProtocol.OperationLock.LockAsync())
      {
        _device.IsConnected = false;
        _device.TypeMode = MultimeterTypeMode.None;
        IsReset?.Invoke();
        return true;
      }
    }

    public Task<bool> ResetAsync(IUserInteractionService userMessageService = null)
    {
      _device.TypeMode = MultimeterTypeMode.None;
      IsReset?.Invoke();
      return Task.FromResult(true);
    }

    public string GetConnectionStatus()
    {
      string connection = _device.IsConnected ? "Подключен" : "Не подключен";
      string mode = _device.TypeMode switch
      {
        MultimeterTypeMode.Resistance => "измерение сопротивления",
        MultimeterTypeMode.None => "режим не задан",
        _ => _device.TypeMode.ToString()
      };

      return $"{connection}. Режим: {mode}.";
    }

    private string GetUsbSearchPattern()
    {
      return string.IsNullOrWhiteSpace(_device.ConnectionDetails)
        ? _device.Name
        : _device.ConnectionDetails;
    }
  }
}
