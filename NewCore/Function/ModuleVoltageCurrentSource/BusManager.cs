using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Dictonary;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Communication.Ethernet.Udp;
using static Ask.LogLib.LoggerUtility;

namespace NewCore.Function.ModuleVoltageCurrentSource
{
  /// <summary>
  /// Управляет подключением шин модуля источника напряжения и тока (МИНТ).
  /// </summary>
  public class BusManager : IBusManager
  {
    /// <summary>
    /// Экземпляр интерфейса модуля источника напряжения и тока.
    /// </summary>
    private readonly IPowerSourceModule _moduleVoltageCurrentSource;

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="BusManager"/>.
    /// </summary>
    /// <param name="moduleVoltageCurrentSource">Экземпляр интерфейса модуля источника напряжения и тока.</param>
    public BusManager(IPowerSourceModule moduleVoltageCurrentSource) => _moduleVoltageCurrentSource = moduleVoltageCurrentSource;

    /// <summary>
    /// Подключает заданную шину МИНТ к положительному полюсу.
    /// </summary>
    /// <param name="bus">Шина, которую необходимо подключить.</param>
    /// <returns>Булево значение, указывающее успешность операции.</returns>
    public async Task<bool> ConnectBusToPositiveAsync(SwitchingBus bus, IUserInteractionService? userMessageService = null)
    {
      if (!DeviceDictonary.BusParameters.TryGetValue(bus, out Tuple<int, int> partialCommand))
      {
        LogError($"Ошибка: Неизвестная шина {bus}", isDeviceLog: true);
        return false;
      }

      LogInformation($"МИНТ: Подключение шины {bus} к + ({new DeviceCommand(5, partialCommand.Item1, partialCommand.Item2, 1)})", isDeviceLog: true);

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      await _moduleVoltageCurrentSource.DeviceProtocol.QueryAsync(new DeviceCommand(5, partialCommand.Item1, partialCommand.Item2, 1).ToString());
      return true;
    }

    /// <summary>
    /// Подключает заданную шину МИНТ к отрицательному полюсу.
    /// </summary>
    /// <param name="bus">Шина, которую необходимо подключить.</param>
    /// <returns>Булево значение, указывающее успешность операции.</returns>
    public async Task<bool> ConnectBusToNegativeAsync(SwitchingBus bus, IUserInteractionService? userMessageService = null)
    {
      if (!DeviceDictonary.BusParameters.TryGetValue(bus, out Tuple<int, int> partialCommand))
      {
        LogError($"Ошибка: Неизвестная шина {bus}", isDeviceLog: true);
        return false;
      }

      LogInformation($"МИНТ: Подключение шины {bus} к - ({new DeviceCommand(6, partialCommand.Item1, partialCommand.Item2, 1)})", isDeviceLog: true);

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      await _moduleVoltageCurrentSource.DeviceProtocol.QueryAsync(new DeviceCommand(6, partialCommand.Item1, partialCommand.Item2, 1).ToString());
      return true;
    }

    /// <summary>
    /// Отключает заданную шину МИНТ от положительного полюса.
    /// </summary>
    /// <param name="bus">Шина, которую необходимо отключить.</param>
    /// <returns>Булево значение, указывающее успешность операции.</returns>
    public async Task<bool> DisconnectBusToPositiveAsync(SwitchingBus bus, IUserInteractionService? userMessageService = null)
    {
      if (!DeviceDictonary.BusParameters.TryGetValue(bus, out Tuple<int, int> partialCommand))
      {
        LogError($"Ошибка: Неизвестная шина {bus}", isDeviceLog: true);
        return false;
      }

      LogInformation($"МИНТ: Отключение шины {bus} от + ({new DeviceCommand(5, partialCommand.Item1, partialCommand.Item2, 2)})", isDeviceLog: true);

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      await _moduleVoltageCurrentSource.DeviceProtocol.QueryAsync(new DeviceCommand(5, partialCommand.Item1, partialCommand.Item2, 2).ToString());
      return true;
    }

    /// <summary>
    /// Отключает заданную шину МИНТ от отрицательного полюса.
    /// </summary>
    /// <param name="bus">Шина, которую необходимо отключить.</param>
    /// <returns>Булево значение, указывающее успешность операции.</returns>
    public async Task<bool> DisconnectBusToNegativeAsync(SwitchingBus bus, IUserInteractionService? userMessageService = null)
    {
      if (!DeviceDictonary.BusParameters.TryGetValue(bus, out Tuple<int, int> partialCommand))
      {
        LogError($"Ошибка: Неизвестная шина {bus}", isDeviceLog: true);
        return false;
      }

      LogInformation($"МИНТ: Отключение шины {bus} от - ({new DeviceCommand(6, partialCommand.Item1, partialCommand.Item2, 2)})", isDeviceLog: true);

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      await _moduleVoltageCurrentSource.DeviceProtocol.QueryAsync(new DeviceCommand(6, partialCommand.Item1, partialCommand.Item2, 2).ToString());
      return true;
    }
  }
}
