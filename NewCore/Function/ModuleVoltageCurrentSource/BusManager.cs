using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NewCore.Base.Function.ModuleVoltageCurrentSource;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using static NewCore.Enum.DeviceEnum;
using static Utilities.LoggerUtility;


namespace NewCore.Function.ModuleVoltageCurrentSource
{
  public class BusManager : IBusManager
  {
    IPowerSourceModule _moduleVoltageCurrentSource { get; set; }
    public BusManager(IPowerSourceModule moduleVoltageCurrentSource) => _moduleVoltageCurrentSource = moduleVoltageCurrentSource;

    /// <summary>
    /// Подключить шину МИНТ к положительному полюсу.
    /// </summary>
    /// <param name="_moduleVoltageCurrentSource.IPAddress">Ip устройства.</param>
    /// <param name="bus">Замыкаемая шина.</param>
    /// <returns>Результат замыкания шины.</returns>
    public async Task<bool> ConnectBusToPositiveAsync(SwitchingBus bus)
    {
      Tuple<int, int> partialComand;
      BusParameters.TryGetValue(bus, out partialComand);
      LogInformation($"МИНТ: Подключение шины {bus.ToString()} к + ({new DeviceCommand(5, partialComand.Item1, partialComand.Item2, 1).ToString()})");
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleVoltageCurrentSource.ConnectionDetails), new DeviceCommand(5, partialComand.Item1, partialComand.Item2, 1));
      return true;
    }

    /// <summary>
    /// Подключить шину МИНТ к отрицательному полюсу.
    /// </summary>
    /// <param name="_moduleVoltageCurrentSource.IPAddress">Ip устройства.</param>
    /// <param name="bus">Замыкаемая шина.</param>
    /// <returns>Результат замыкания шины.</returns>
    public async Task<bool> ConnectBusToNegativeAsync(SwitchingBus bus)
    {
      Tuple<int, int> partialComand;
      BusParameters.TryGetValue(bus, out partialComand);
      LogInformation($"МИНТ: Подключение шины {bus.ToString()} к - ({new DeviceCommand(6, partialComand.Item1, partialComand.Item2, 1).ToString()})");
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleVoltageCurrentSource.ConnectionDetails), new DeviceCommand(6, partialComand.Item1, partialComand.Item2, 1));
      return true;
    }

    /// <summary>
    /// Отключает шину МИНТ от положительного полюса.
    /// </summary>
    /// <param name="_moduleVoltageCurrentSource.IPAddress">Ip устройства.</param>
    /// <param name="bus">Отключаемая шина.</param>
    /// <returns>Результат замыкания шины.</returns>
    public async Task<bool> DisconnectBusToPositiveAsync(SwitchingBus bus)
    {
      Tuple<int, int> partialComand;
      BusParameters.TryGetValue(bus, out partialComand);
      LogInformation($"МИНТ: Отключение шины {bus.ToString()} от + ({new DeviceCommand(5, partialComand.Item1, partialComand.Item2, 2).ToString()})");
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleVoltageCurrentSource.ConnectionDetails), new DeviceCommand(5, partialComand.Item1, partialComand.Item2, 2));
      return true;
    }

    /// <summary>
    /// Отключает шину МИНТ от отрицательному полюса.
    /// </summary>
    /// <param name="_moduleVoltageCurrentSource.IPAddress">Ip устройства.</param>
    /// <param name="bus">Отключаемая шина.</param>
    /// <returns>Результат замыкания шины.</returns>
    public async Task<bool> DisconnectBusToNegativeAsync(SwitchingBus bus)
    {
      Tuple<int, int> partialComand;
      BusParameters.TryGetValue(bus, out partialComand);
      LogInformation($"МИНТ: Отключение шины {bus.ToString()} от - ({new DeviceCommand(6, partialComand.Item1, partialComand.Item2, 2).ToString()})");
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleVoltageCurrentSource.ConnectionDetails), new DeviceCommand(6, partialComand.Item1, partialComand.Item2, 2));
      return true;
    }
  }
}
