using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NewCore.Base.DeviceResponses;
using NewCore.Base.Function.ModuleVoltageCurrentSource;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using NewCore.Device;

namespace NewCore.Function.ModuleVoltageCurrentSource
{
  public class StateManager : IStateManager
  {
    IPowerSourceModule _moduleVoltageCurrentSource { get; set; }
    public StateManager(IPowerSourceModule moduleVoltageCurrentSource) => _moduleVoltageCurrentSource = moduleVoltageCurrentSource;


    /// <summary>
    /// Инициализация модуля коммутации реле.
    /// </summary>
    /// <returns>Возвращает ответ, получен ли ответ от инициализации.</returns>
    public async Task<(bool Connect, string Answer)> Initialize()
    {
      DeviceCommand cmd = new DeviceCommand(1, 0, 0, 0);
      string result = await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleVoltageCurrentSource.ConnectionDetails), cmd, 2000).ConfigureAwait(true);

      BaseResponse baseResponse = BaseResponse.FromJson(result);
      if (baseResponse != null)
      {
        if (baseResponse.NumberChassis == _moduleVoltageCurrentSource.NumberChassis &&
      baseResponse.NumberDevice == _moduleVoltageCurrentSource.Number)
        {
          return (true, result);
        }
        else
        {
          string errorMessage = string.Empty;

          if (baseResponse.NumberChassis != _moduleVoltageCurrentSource.NumberChassis)
          {
            errorMessage += $"Несовпадение по NumberChassis: ожидается {_moduleVoltageCurrentSource.NumberChassis}, получено {baseResponse.NumberChassis}. ";
          }
          if (baseResponse.NumberDevice != _moduleVoltageCurrentSource.Number)
          {
            errorMessage += $"Несовпадение по NumberDevice: ожидается {_moduleVoltageCurrentSource.Number}, получено {baseResponse.NumberDevice}.";
          }

          return (false, errorMessage.Trim());
        }
      }

      return (false, result);
    }

    /// <summary>
    /// Выполняет сброс всех реле на МКР.
    /// </summary>
    /// <param name="_moduleRelayControl.IPAddress">IP адрес.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<bool> ResetAsync()
    {
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleVoltageCurrentSource.ConnectionDetails), new DeviceCommand(2));
      return true;
    }
  }
}
