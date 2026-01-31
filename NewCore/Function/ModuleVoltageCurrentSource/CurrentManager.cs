using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Communication;
using static Ask.LogLib.LoggerUtility;

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
    public async Task SetCurrentLevelAsync(int integerPart, int decimalPart, IUserInteractionService? messageService = null)
    {
      LogInformation($"МИНТ: Установка тока {integerPart}.{decimalPart} мА ({new DeviceCommand(4, integerPart, decimalPart)})", isDeviceLog: true);

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return;
      }

      await _moduleVoltageCurrentSource.DeviceProtocol.QueryAsync(new DeviceCommand(4, integerPart, decimalPart).ToString(), timeout: 2000);
    }

    /// <summary>
    /// Устанавливает ограничение выдаваемого тока для модуля источника напряжения и тока (МИНТ).
    /// </summary>
    /// <param name="current">Ограничение тока в мА.</param>
    /// <returns>Булево значение, указывающее успешность операции.</returns>
    public async Task<bool> LimitationOfTheOutputCurrent(int current, IUserInteractionService? messageService = null)
    {
      LogInformation($"МИНТ: Установка ограничения тока в {current} мА ({new DeviceCommand(10, current)})", isDeviceLog: true);

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      await _moduleVoltageCurrentSource.DeviceProtocol.QueryAsync(new DeviceCommand(10, current).ToString(), timeout: 2000);
      return true;
    }
  }
}
