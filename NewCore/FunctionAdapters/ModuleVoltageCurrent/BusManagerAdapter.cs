using System;
using System.Threading.Tasks;
using NewCore.Base.Function.ModuleVoltageCurrentSource;
using NewCore.Base.Interface.Main;
using NewCore.Function.Helpers;
using NewCore.Function.ModuleVoltageCurrentSource;
using static NewCore.Enum.DeviceEnum;
using static Utilities.LoggerUtility;

namespace NewCore.FunctionAdapters.ModuleVoltageCurrentSource
{
  /// <summary>
  /// Адаптер для управления подключением шин МИНТ с отображением сообщений.
  /// </summary>
  internal class BusManagerAdapter : IBusManager
  {
    private readonly IPowerSourceModule _module;
    private readonly BusManager _busManager;

    public BusManagerAdapter(IPowerSourceModule module)
    {
      _module = module ?? throw new ArgumentNullException(nameof(module));
      _busManager = new BusManager(module);
    }

    public async Task<bool> ConnectBusToPositiveAsync(SwitchingBus bus)
    {
      bool result = await _busManager.ConnectBusToPositiveAsync(bus);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_module, "Подключение к +", bus.ToString(), result, 1);
      return result;
    }

    public async Task<bool> ConnectBusToNegativeAsync(SwitchingBus bus)
    {
      bool result = await _busManager.ConnectBusToNegativeAsync(bus);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_module, "Подключение к -", bus.ToString(), result, 1);
      return result;
    }

    public async Task<bool> DisconnectBusToPositiveAsync(SwitchingBus bus)
    {
      bool result = await _busManager.DisconnectBusToPositiveAsync(bus);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_module, "Отключение от +", bus.ToString(), result, 1);
      return result;
    }

    public async Task<bool> DisconnectBusToNegativeAsync(SwitchingBus bus)
    {
      bool result = await _busManager.DisconnectBusToNegativeAsync(bus);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_module, "Отключение от -", bus.ToString(), result, 1);
      return result;
    }
  }
}
