using NewCore.Base.Device;
using NewCore.Base.DeviceResponses;
using NewCore.Base.Function.DBC;
using NewCore.Communication;
using static AppConfiguration.Execution.ExecutionConfig;

namespace NewCore.Function.DeviceBusCommutation
{
  /// <summary>
  /// Класс для управления состоянием устройства коммутации шин.
  /// </summary>
  public class StateManager : IConnectable
  {
    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="StateManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public StateManager(Device.DeviceBusCommutation deviceBusCommutation) => _deviceBusCommutation = deviceBusCommutation;

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync()
    {
      return await InitializeAsync();
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync()
    {
      return await ResetAsync();
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync()
    {
      if (await GetIsIdleModeEnabled())
      {
        return (true, "Включен холостой режим");
      }

      DeviceCommand cmd = new DeviceCommand(1, 1, 1, 1);
      string result = await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 2000);

      BaseResponse baseResponse = BaseResponse.FromJson(result);
      if (baseResponse != null)
      {
        if (baseResponse.NumberChassis == _deviceBusCommutation.NumberChassis &&
      baseResponse.NumberDevice == _deviceBusCommutation.Number)
        {
          return (true, result);
        }
        else
        {
          string errorMessage = string.Empty;

          if (baseResponse.NumberChassis != _deviceBusCommutation.NumberChassis)
          {
            errorMessage += $"Несовпадение по NumberChassis: ожидается {_deviceBusCommutation.NumberChassis}, получено {baseResponse.NumberChassis}. ";
          }

          if (baseResponse.NumberDevice != _deviceBusCommutation.Number)
          {
            errorMessage += $"Несовпадение по NumberDevice: ожидается {_deviceBusCommutation.Number}, получено {baseResponse.NumberDevice}.";
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

      DeviceCommand cmd = new DeviceCommand(2, 0, 0, 0);
      string result = await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString());
      return result == "2.0.1";
    }
  }
}
