using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.ModuleVoltageCurrent;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using NewCore.Function.Helpers;
using NewCore.Function.ModuleVoltageCurrentSource;

namespace NewCore.FunctionAdapters.ModuleVoltageCurrent
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
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _busManager.ConnectBusToPositiveAsync(bus), userMessageService, deviceTask: true);
      
      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_module, "Подключение к +", bus.ToString(), result, 1, userMessageService);
      }

      if (!result)
        throw BusExceptionFactory.ConnectPositiveFailed(bus.ToString());

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectBusToNegativeAsync(SwitchingBus bus, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _busManager.ConnectBusToNegativeAsync(bus), userMessageService, deviceTask: true);

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_module, "Подключение к -", bus.ToString(), result, 1, userMessageService);
      }

      if (!result)
        throw BusExceptionFactory.ConnectNegativeFailed(bus.ToString());

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectBusToPositiveAsync(SwitchingBus bus, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _busManager.DisconnectBusToPositiveAsync(bus), userMessageService, deviceTask: true);

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_module, "Отключение от +", bus.ToString(), result, 1, userMessageService);
      }

      if (!result)
        throw BusExceptionFactory.DisconnectPositiveFailed(bus.ToString());

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectBusToNegativeAsync(SwitchingBus bus, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _busManager.DisconnectBusToNegativeAsync(bus), userMessageService, deviceTask: true);

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_module, "Отключение от -", bus.ToString(), result, 1, userMessageService);
      }

      if (!result)
        throw BusExceptionFactory.DisconnectNegativeFailed(bus.ToString());

      return result;
    }
  }
}
