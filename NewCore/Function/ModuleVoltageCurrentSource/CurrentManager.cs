using System.Net;
using NewCore.Base.Function.ModuleVoltageCurrentSource;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using NewCore.Device;
using static Utilities.LoggerUtility;
using static AppConfiguration.Execution.ExecutionConfig;

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

      if (await GetIsIdleModeEnabled())
      {
        return;
      }

      await _moduleVoltageCurrentSource.DeviceProtocol.QueryAsync(new DeviceCommand(4, integerPart, decimalPart).ToString());
    }

    /// <summary>
    /// Устанавливает ограничение выдаваемого тока для модуля источника напряжения и тока (МИНТ).
    /// </summary>
    /// <param name="current">Ограничение тока в мА.</param>
    /// <returns>Булево значение, указывающее успешность операции.</returns>
    public async Task<bool> LimitationOfTheOutputCurrent(int current)
    {
      LogInformation($"МИНТ: Установка ограничения тока в {current} мА ({new DeviceCommand(10, current)})");

      if (await GetIsIdleModeEnabled())
      {
        return true;
      }

      await _moduleVoltageCurrentSource.DeviceProtocol.QueryAsync(new DeviceCommand(10, current).ToString(), timeout: 2000);
      return true;
    }
  }
}
