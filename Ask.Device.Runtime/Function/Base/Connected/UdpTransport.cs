using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Base.Device;
using Ask.Device.Runtime.Function.ManagerChassis;
using Ask.Device.Runtime.Function.ModuleRelayControl;

namespace Ask.Device.Runtime.Function.Connected
{
  /// <summary>
  /// Реализует подключение и обмен данными с устройством по протоколу UDP/IP.
  /// </summary>
  internal class UdpTransport : IConnectable
  {
    /// <summary>
    /// Устройство, с которым выполняется взаимодействие.
    /// </summary>
    private readonly DeviceWithUdpIp _device;

    /// <summary>
    /// Возникает после выполнения операции сброса устройства.
    /// </summary>
    public event Action? IsReset;

    /// <summary>
    /// Инициализирует транспорт UDP/IP для указанного устройства.
    /// </summary>
    /// <param name="device">Устройство, работающее по протоколу UDP/IP.</param>
    public UdpTransport(DeviceWithUdpIp device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService userMessageService = null)
    {
      if (_device is IRelaySwitchModule module)
      {
        string response = await new ModuleRelayControlQueryExecutor(module).QueryAsync(
          _device.ConnectedProfile.Initialize,
          _device.ConnectedProfile.Timeout);
        return ValidateInitialization(response);
      }

      if (_device is IChassisManager chassis)
      {
        string response = await new ChassisQueryExecutor(chassis).QueryAsync(
          _device.ConnectedProfile.Initialize,
          _device.ConnectedProfile.Timeout);
        return ValidateInitialization(response);
      }

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return IdleHardwareErrorSimulator.ShouldSimulateHardwareError()
          ? (false, IdleHardwareErrorSimulator.ErrorMessage)
          : (true, string.Empty);
      }

      string result = await _device.DeviceProtocol.QueryAsync(_device.ConnectedProfile.Initialize, timeout: _device.ConnectedProfile.Timeout);
      return ValidateInitialization(result);
    }

    private (bool Connect, string Answer) ValidateInitialization(string response)
    {
      if (string.IsNullOrEmpty(response))
      {
        IsReset?.Invoke();
        return (false, $"Нет ответа от устройства {_device.Name}({_device.Number})");
      }

      var initializationResult = _device.InitializationValidationDelegate(response, _device);
      if (initializationResult.Success)
      {
        return (true, string.Empty);
      }

      IsReset?.Invoke();
      return (false, initializationResult.Message);
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService userMessageService = null)
    {
      return await InitializeAsync();
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync(IUserInteractionService userMessageService = null)
    {
      return await ResetAsync();
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync(IUserInteractionService userMessageService = null)
    {
      if (_device is IRelaySwitchModule module)
      {
        string response = await new ModuleRelayControlQueryExecutor(module).QueryAsync(
          _device.ConnectedProfile.Reset,
          _device.ConnectedProfile.Timeout);
        return ValidateReset(response);
      }

      if (_device is IChassisManager chassis)
      {
        string response = await new ChassisQueryExecutor(chassis).QueryAsync(
          _device.ConnectedProfile.Reset,
          _device.ConnectedProfile.Timeout);
        return ValidateReset(response);
      }

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        if (IdleHardwareErrorSimulator.ShouldSimulateHardwareError())
        {
          return false;
        }

        IsReset?.Invoke();
        return true;
      }

      string result = await _device.DeviceProtocol.QueryAsync(_device.ConnectedProfile.Reset, timeout: _device.ConnectedProfile.Timeout);
      return ValidateReset(result);
    }

    private bool ValidateReset(string response)
    {
      var resetResult = _device.ResetValidationDelegate(response, _device);
      IsReset?.Invoke();

      return resetResult;
    }

  }
}
