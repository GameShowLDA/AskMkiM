using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Communication.Ethernet.Udp;
using NewCore.Communication;
using static Ask.LogLib.LoggerUtility;

namespace NewCore.Function.ModuleVoltageCurrentSource
{
  /// <summary>
  /// Класс для управления напряжением на модуле источника напряжения и тока (МИНТ).
  /// </summary>
  public class VoltageManager : IVoltageManager
  {
    private readonly IPowerSourceModule _moduleVoltageCurrentSource;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="VoltageManager"/>.
    /// </summary>
    /// <param name="moduleVoltageCurrentSource">Модуль источника напряжения и тока, для которого будет выполняться управление напряжением.</param>
    public VoltageManager(IPowerSourceModule moduleVoltageCurrentSource) => _moduleVoltageCurrentSource = moduleVoltageCurrentSource;

    /// <summary>
    /// Устанавливает источник напряжения на МИНТ.
    /// </summary>
    /// <param name="voltageSources">Источник напряжения (например, 12В или 5В).</param>
    /// <returns>Асинхронная задача, представляющая операцию установки источника напряжения.</returns>
    public async Task SetSourceVoltageAsync(VoltageSources voltageSources, IUserInteractionService? messageService = null)
    {
      LogInformation($"Устанавливаем источник питания {(voltageSources == VoltageSources.Supply12V ? "12В" : "5В")}", isDeviceLog: true);

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return;
      }

      var cmd = new DeviceCommand(9, voltageSources == VoltageSources.Supply12V ? 1 : 0);
      await _moduleVoltageCurrentSource.DeviceProtocol.QueryAsync(cmd.ToString());
    }

    /// <summary>
    /// Устанавливает дискретное значение напряжения на МИНТ.
    /// </summary>
    /// <param name="integerPart">Целая часть напряжения.</param>
    /// <param name="decimalPart">Дробная часть напряжения.</param>
    /// <returns>Асинхронная задача, представляющая операцию установки напряжения.</returns>
    public async Task SetVoltageLevelAsync(int integerPart, int decimalPart, IUserInteractionService? messageService = null)
    {
      LogInformation($"Устанавливаем напряжение {integerPart}.{decimalPart} В ({new DeviceCommand(3, integerPart, decimalPart).ToString()})", isDeviceLog: true);

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return;
      }

      var cmd = new DeviceCommand(3, integerPart, decimalPart);
      await _moduleVoltageCurrentSource.DeviceProtocol.QueryAsync(cmd.ToString());
    }
  }
}
