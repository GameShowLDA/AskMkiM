using System;
using System.Threading.Tasks;
using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using NewCore.Function.Helpers;
using NewCore.Function.ModuleRelayControl;
using static NewCore.Enum.DeviceEnum;

namespace NewCore.FunctionAdapters.ModuleRelayControl
{
  /// <summary>
  /// Адаптер управления подключением и отключением шин МКР с сообщениями.
  /// </summary>
  internal class BusManagerAdapter : IBusManager
  {
    private readonly IRelaySwitchModule _moduleRelayControl;
    private readonly BusManager _busManager;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BusManagerAdapter"/>.
    /// </summary>
    /// <param name="moduleRelayControl">Модуль релейного управления.</param>
    public BusManagerAdapter(IRelaySwitchModule moduleRelayControl)
    {
      _moduleRelayControl = moduleRelayControl ?? throw new ArgumentNullException(nameof(moduleRelayControl));
      _busManager = new BusManager(_moduleRelayControl);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectBusAsync(SwitchingBus bus, bool lowVoltage)
    {
      var result = await _busManager.ConnectBusAsync(bus, lowVoltage);

      var type = lowVoltage ? "низковольтной" : "высоковольтной";
      var message = $"Подключение {type} шины [{bus}]";

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _moduleRelayControl,
        message,
        result,
        1);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectBusAsync(SwitchingBus bus, bool lowVoltage)
    {
      var result = await _busManager.DisconnectBusAsync(bus, lowVoltage);

      var type = lowVoltage ? "низковольтной" : "высоковольтной";
      var message = $"Отключение {type} шины [{bus}]";

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _moduleRelayControl,
        message,
        result,
        1);

      return result;
    }

    /// <inheritdoc />
    public bool TryGetBusNumber(SwitchingBus bus, out int busNumber)
    {
      return _busManager.TryGetBusNumber(bus, out busNumber);
    }

    /// <inheritdoc />
    public bool TryGetBusType(SwitchingBus bus, out int busType)
    {
      return _busManager.TryGetBusType(bus, out busType);
    }
  }
}
