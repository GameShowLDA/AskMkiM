using NewCore.Base.DeviceResponses;
using NewCore.Base.Function.DBC;
using NewCore.Communication;

namespace NewCore.Function.DeviceBusCommutation
{
  /// <summary>
  /// Класс для управления состоянием устройства коммутации шин.
  /// </summary>
  public class StateManager : IStateDeviceBusCommutation
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
    public async Task<(bool Connect, string Answer)> Initialize()
    {
      DeviceCommand cmd = new DeviceCommand(1, 1, 1, 1);
      string result = await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, cmd, 2000).ConfigureAwait(true);

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
      DeviceCommand cmd = new DeviceCommand(2, 0, 0, 0);
      string result = await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, cmd, 1000).ConfigureAwait(true);
      return result == "2.0.1";
    }
  }
}
