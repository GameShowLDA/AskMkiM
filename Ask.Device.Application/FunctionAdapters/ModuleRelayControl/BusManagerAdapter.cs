
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Base.Device;
using Ask.Device.Runtime.Function.Helpers;
using Ask.Device.Runtime.Function.ModuleRelayControl;

namespace Ask.Device.Application.FunctionAdapters.ModuleRelayControl
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
    public async Task<bool> ConnectBusAsync(SwitchingBus bus, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _busManager.ConnectBusAsync(bus);
        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_moduleRelayControl, $"Подключение шины [{bus}]", succes, 1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectBusAsync(SwitchingBus bus, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () => 
      {
        var succes = await _busManager.DisconnectBusAsync(bus);
        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_moduleRelayControl, $"Отключение шины [{bus}]", succes, 1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      return result;
    }

    public IReadOnlyList<BusConnectionInfo> GetConnectedBuses() => _busManager.GetConnectedBuses();

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
