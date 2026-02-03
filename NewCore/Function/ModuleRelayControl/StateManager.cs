using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Base.DeviceResponses;
using NewCore.Communication;

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

    public event Action DeviceDisponce;
    public event Action IsReset;

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="StateManager"/>.
    /// </summary>
    /// <param name="moduleRelayControl">Экземпляр интерфейса модуля реле.</param>
    public StateManager(Device.ModuleRelayControl moduleRelayControl) => _moduleRelayControl = moduleRelayControl;

    public StateManager()
    {
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService messageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
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
    public async Task<bool> ResetAsync(IUserInteractionService messageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }


      DeviceCommand cmd = new DeviceCommand(2, 1, 0, 0);
      string result = await _moduleRelayControl.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 1000);

      BaseResponse baseResponse = BaseResponse.FromJson(result);
      if (baseResponse != null)
      {
        if (baseResponse.NumberChassis == _moduleRelayControl.NumberChassis &&
      baseResponse.NumberDevice == _moduleRelayControl.Number && baseResponse.Answer.Contains("2.0"))
        {
          IsReset?.Invoke();
          return true;
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

          return false;
        }
      }

      return false;
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService messageService = null)
    {
      return await InitializeAsync();
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync(IUserInteractionService messageService = null)
    {
      return await ResetAsync();
    }

    public string GetConnectionStatus()
    {
      throw new NotImplementedException();
    }
  }
}
