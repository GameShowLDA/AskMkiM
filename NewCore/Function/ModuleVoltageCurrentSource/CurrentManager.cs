using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NewCore.Base.Function.ModuleVoltageCurrentSource;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using static Utilities.LoggerUtility;

namespace NewCore.Function.ModuleVoltageCurrentSource
{
  public class CurrentManager : ICurrentManager
  {
    IPowerSourceModule _moduleVoltageCurrentSource { get; set; }
    public CurrentManager(IPowerSourceModule moduleVoltageCurrentSource) => _moduleVoltageCurrentSource = moduleVoltageCurrentSource;


    /// <summary>
    /// Устанавливает дискретное значение тока на МИНТ.
    /// </summary>
    /// <param name="_moduleVoltageCurrentSource.IPAddress">IP-адрес МИНТ, на который отправляется команда.</param>
    /// <param name="integerPart">Целая часть значения тока.</param>
    /// <param name="decimalPart">Дробная часть значения тока.</param>
    /// <returns>Асинхронная задача.</returns>
    public async Task SetCurrentLevelAsync(int integerPart, int decimalPart)
    {
      LogInformation($"Устанавливаем ток {integerPart}.{decimalPart} мА ({new DeviceCommand(4, integerPart, decimalPart).ToString()})");
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleVoltageCurrentSource.ConnectionDetails), new DeviceCommand(4, integerPart, decimalPart));
    }

    /// <summary>
    /// Устанавливает ограничение выдаваемого тока для ПИН.
    /// </summary>
    /// <param name="_moduleVoltageCurrentSource.IPAddress">Ip устройства.</param>
    /// <param name="current">Ток в мА.</param>
    /// <returns>Результат установки тока.</returns>
    public async Task<bool> LimitationOfTheOutputCurrent(int current)
    {
      LogInformation($"МИНТ: Установка ограничения тока в {current}мА от - ({new DeviceCommand(10, current).ToString()})");
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleVoltageCurrentSource.ConnectionDetails), new DeviceCommand(10, current));
      return true;
    }
  }
}
