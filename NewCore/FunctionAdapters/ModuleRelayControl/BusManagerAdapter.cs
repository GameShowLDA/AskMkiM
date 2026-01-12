
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using NewCore.Function.Helpers;
using NewCore.Function.ModuleRelayControl;

namespace NewCore.FunctionAdapters.ModuleRelayControl
{
  /// <summary>
  /// Адаптер управления подключением и отключением шин МКР с сообщениями.
  /// </summary>
  internal class BusManagerAdapter : IBusManager
  {
    private readonly IRelaySwitchModule _moduleRelayControl;
    private readonly BusManager _busManager;
    private readonly Dictionary<SwitchingBus, bool> switchingBuses = new Dictionary<SwitchingBus, bool>();

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BusManagerAdapter"/>.
    /// </summary>
    /// <param name="moduleRelayControl">Модуль релейного управления.</param>
    public BusManagerAdapter(IRelaySwitchModule moduleRelayControl)
    {
      _moduleRelayControl = moduleRelayControl ?? throw new ArgumentNullException(nameof(moduleRelayControl));
      _busManager = new BusManager(_moduleRelayControl);
      _moduleRelayControl.ConnectableManager.IsReset += ConnectableManager_IsReset;

      ConnectableManager_IsReset();
    }

    private void ConnectableManager_IsReset()
    {
      switchingBuses.Clear();
      foreach (SwitchingBus item in System.Enum.GetValues(typeof(SwitchingBus)))
      {
        switchingBuses.Add(item, false);
      }
    }

    /// <inheritdoc />
    public async Task<bool> ConnectBusAsync(SwitchingBus bus, bool lowVoltage, IUserInteractionService? userMessageService = null)
    {
      switchingBuses.TryGetValue(bus, out bool connected);
      if (connected)
      {
        return true;
      }

      var type = lowVoltage ? "низковольтной" : "высоковольтной";
      var description = $"{type} шины [{bus}]";

      var result = await _busManager.ConnectBusAsync(bus, lowVoltage);

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _moduleRelayControl,
            $"Подключение {description}",
            result,
            1, userMessageService);
      }

      if (result)
      {
        switchingBuses[bus] = true;
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectBusAsync(SwitchingBus bus, bool lowVoltage, IUserInteractionService? userMessageService = null)
    {
      switchingBuses.TryGetValue(bus, out bool connected);
      if (!connected)
        return true;

      var type = lowVoltage ? "низковольтной" : "высоковольтной";
      var description = $"{type} шины [{bus}]";

      var result = await _busManager.DisconnectBusAsync(bus, lowVoltage);

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_moduleRelayControl, $"Отключение {description}", result, 1, userMessageService);
      }

      if (result)
      {
        switchingBuses[bus] = false;
      }

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
