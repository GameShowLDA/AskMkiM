using System.Net;
using NewCore.Base.Device;
using NewCore.Base.DeviceResponses;
using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using NewCore.Device;
using static AppConfiguration.Execution.ExecutionConfig;

namespace NewCore.Function.ModuleRelayControl
{
  /// <summary>
  /// Управляет состоянием модуля коммутации реле (МКР).
  /// </summary>
  public class StateManager : IConnectable
  {
    /// <summary>
    /// Экземпляр интерфейса модуля реле.
    /// </summary>
    private readonly Device.ModuleRelayControl _moduleRelayControl;

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="StateManager"/>.
    /// </summary>
    /// <param name="moduleRelayControl">Экземпляр интерфейса модуля реле.</param>
    public StateManager(Device.ModuleRelayControl moduleRelayControl) => _moduleRelayControl = moduleRelayControl;

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync()
    {
      if (await GetIsIdleModeEnabled())
      {
        return (true, String.Empty);
      }

      DeviceCommand cmd = new DeviceCommand(1, 0, 0, 0);
      string result = await _moduleRelayControl.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 2000);

      if (string.IsNullOrEmpty(result))
      {
        return (false, $"Нет ответа от устройства {_moduleRelayControl.Name}({_moduleRelayControl.Number})");
      }

      BaseResponse baseResponse = BaseResponse.FromJson(result);
      if (baseResponse != null)
      {
        if (baseResponse.NumberChassis == _moduleRelayControl.NumberChassis &&
      baseResponse.NumberDevice == _moduleRelayControl.Number)
        {
          return (true, result);
        }
        else
        {
          string errorMessage = string.Empty;

          if (baseResponse.NumberChassis != _moduleRelayControl.NumberChassis)
          {
            errorMessage += $"Несовпадение по NumberChassis: ожидается {_moduleRelayControl.NumberChassis}, получено {baseResponse.NumberChassis}. ";
          }

          if (baseResponse.NumberDevice != _moduleRelayControl.Number)
          {
            errorMessage += $"Несовпадение по NumberDevice: ожидается {_moduleRelayControl.Number}, получено {baseResponse.NumberDevice}.";
          }

          return (false, errorMessage.Trim());
        }
      }

      return (false, result);
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync()
    {
      if (await GetIsIdleModeEnabled())
      {
        return true;
      }


      DeviceCommand cmd = new DeviceCommand(2, 1, 0, 0);
      string result = await _moduleRelayControl.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 1000);
      return result == "2.0.1";
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync()
    {
      return await InitializeAsync();
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync()
    {
      await _moduleRelayControl.DeviceProtocol.OperationLock.WaitAsync();
      return await ResetAsync();
    }
  }
}
