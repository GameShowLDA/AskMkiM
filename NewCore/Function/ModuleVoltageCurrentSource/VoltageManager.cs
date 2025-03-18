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
  public class VoltageManager : IVoltageManager
  {
    IPowerSourceModule _moduleVoltageCurrentSource { get; set; }
    public VoltageManager(IPowerSourceModule moduleVoltageCurrentSource) => _moduleVoltageCurrentSource = moduleVoltageCurrentSource;

    /// <summary>
    /// Устанавливает дискретное значение напряжения на МИНТ.
    /// </summary>
    /// <param name="_moduleVoltageCurrentSource.IPAddress">IP-адрес МИНТ, на который отправляется команда.</param>
    /// <param name="voltageSources">Источник напряжения.</param>
    /// <returns>Асинхронная задача.</returns>
    public async Task SetSourceVoltageAsync(VoltageSources voltageSources)
    {
      LogInformation($"Устанавливаем иточник питания {(voltageSources == VoltageSources.Supply12V ? "12В" : "5В")}");
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleVoltageCurrentSource.ConnectionDetails), new DeviceCommand(9, voltageSources == VoltageSources.Supply12V ? 1 : 0));
    }

    /// <summary>
    /// Устанавливает дискретное значение напряжения на МИНТ.
    /// </summary>
    /// <param name="_moduleVoltageCurrentSource.IPAddress">IP-адрес МИНТ, на который отправляется команда.</param>
    /// <param name="integerPart">Целая часть напряжения.</param>
    /// <param name="decimalPart">Дробная часть напряжения.</param>
    /// <returns>Асинхронная задача.</returns>
    public async Task SetVoltageLevelAsync(int integerPart, int decimalPart)
    {
      LogInformation($"Устанавливаем напряжение {integerPart}.{decimalPart} В ({new DeviceCommand(3, integerPart, decimalPart).ToString()})");
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleVoltageCurrentSource.ConnectionDetails), new DeviceCommand(3, integerPart, decimalPart));
    }
  }
}
