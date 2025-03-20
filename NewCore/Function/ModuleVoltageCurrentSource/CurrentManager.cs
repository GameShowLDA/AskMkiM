using System;
using System.Net;
using System.Threading.Tasks;
using NewCore.Base.Function.ModuleVoltageCurrentSource;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using static Utilities.LoggerUtility;

namespace NewCore.Function.ModuleVoltageCurrentSource
{
  /// <summary>
  /// Класс для управления током на модуле источника напряжения и тока (МИНТ).
  /// </summary>
  public class CurrentManager : ICurrentManager
  {
    /// <summary>
    /// Экземпляр интерфейса модуля источника напряжения и тока.
    /// </summary>
    private readonly IPowerSourceModule _moduleVoltageCurrentSource;

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="CurrentManager"/>.
    /// </summary>
    /// <param name="moduleVoltageCurrentSource">Экземпляр интерфейса модуля источника напряжения и тока.</param>
    public CurrentManager(IPowerSourceModule moduleVoltageCurrentSource) => _moduleVoltageCurrentSource = moduleVoltageCurrentSource;

    /// <summary>
    /// Устанавливает дискретное значение тока на МИНТ.
    /// </summary>
    /// <param name="integerPart">Целая часть значения тока.</param>
    /// <param name="decimalPart">Дробная часть значения тока.</param>
    /// <returns>Асинхронная задача.</returns>
    public async Task SetCurrentLevelAsync(int integerPart, int decimalPart)
    {
      LogInformation($"МИНТ: Установка тока {integerPart}.{decimalPart} мА ({new DeviceCommand(4, integerPart, decimalPart)})");

      await DeviceCommandSender.SendCommandAsync(
          IPAddress.Parse(_moduleVoltageCurrentSource.ConnectionDetails),
          new DeviceCommand(4, integerPart, decimalPart)
      );
    }

    /// <summary>
    /// Устанавливает ограничение выдаваемого тока для модуля источника напряжения и тока (МИНТ).
    /// </summary>
    /// <param name="current">Ограничение тока в мА.</param>
    /// <returns>Булево значение, указывающее успешность операции.</returns>
    public async Task<bool> LimitationOfTheOutputCurrent(int current)
    {
      LogInformation($"МИНТ: Установка ограничения тока в {current} мА ({new DeviceCommand(10, current)})");

      await DeviceCommandSender.SendCommandAsync(
          IPAddress.Parse(_moduleVoltageCurrentSource.ConnectionDetails),
          new DeviceCommand(10, current));

      return true;
    }
  }
}
