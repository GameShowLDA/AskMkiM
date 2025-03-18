using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using NewCore.Communication;

namespace NewCore.Function.ModuleRelayControl
{
  public class MeterManager : IMeterManager
  {
    IRelaySwitchModule _moduleRelayControl { get; set; }
    public MeterManager(IRelaySwitchModule moduleRelayControl) => _moduleRelayControl = moduleRelayControl;

    /// <summary>
    /// Включает измеритель модуля МКР.
    /// </summary>
    /// <returns>Возвращает true, если команда отправлена успешно.</returns>
    /// <remarks>
    /// Этот метод формирует и отправляет команду на включение измерителя модуля МКР по указанному IP-адресу.
    /// </remarks>
    public async Task<bool> ConnectMeterAsync()
    {
      DeviceCommand cmd = new DeviceCommand(5, 1);
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleRelayControl.ConnectionDetails), cmd);
      return true;
    }

    /// <summary>
    /// Отключает измеритель модуля МКР.
    /// </summary>
    /// <returns>Возвращает true, если команда отправлена успешно.</returns>
    /// <remarks>
    /// Этот метод формирует и отправляет команду на отключение измерителя модуля МКР по указанному IP-адресу.
    /// </remarks>
    public async Task<bool> DisconnectMeterAsync()
    {
      DeviceCommand cmd = new DeviceCommand(5, 2);
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleRelayControl.ConnectionDetails), cmd);
      return true;
    }

    /// <summary>
    /// Получить ответ от измерителя о замыкании шин или точек.
    /// </summary>
    /// <returns>true если есть замыкание, false если нет.</returns>
    public async Task<bool> GetMeterResponseAsync()
    {
      DeviceCommand cmd = new DeviceCommand(7);
      return (await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleRelayControl.ConnectionDetails), cmd, 1000)).Contains("105.1");
    }
  }
}
