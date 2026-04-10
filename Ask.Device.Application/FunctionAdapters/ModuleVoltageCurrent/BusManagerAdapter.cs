using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.ModuleVoltageCurrent;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Function.Helpers;
using Ask.Device.Runtime.Function.ModuleVoltageCurrentSource;

namespace Ask.Device.Application.FunctionAdapters.ModuleVoltageCurrent
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

    /// <inheritdoc />
    public async Task<bool> ConnectBusToPositiveAsync(SwitchingBus bus, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _busManager.ConnectBusToPositiveAsync(bus);
        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_module, "Подключение к +", bus.ToString(), succes, 1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw BusExceptionFactory.ConnectPositiveFailed(bus.ToString());

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectBusToNegativeAsync(SwitchingBus bus, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _busManager.ConnectBusToNegativeAsync(bus);

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_module, "Подключение к -", bus.ToString(), succes, 1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw BusExceptionFactory.ConnectNegativeFailed(bus.ToString());

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectBusToPositiveAsync(SwitchingBus bus, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _busManager.DisconnectBusToPositiveAsync(bus);
        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_module, "Отключение от +", bus.ToString(), succes, 1, userMessageService);
        }
        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw BusExceptionFactory.DisconnectPositiveFailed(bus.ToString());

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectBusToNegativeAsync(SwitchingBus bus, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _busManager.DisconnectBusToNegativeAsync(bus);
        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_module, "Отключение от -", bus.ToString(), succes, 1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw BusExceptionFactory.DisconnectNegativeFailed(bus.ToString());

      return result;
    }
  }
}
