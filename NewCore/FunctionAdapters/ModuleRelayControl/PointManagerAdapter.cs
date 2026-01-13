using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using NewCore.Function.Helpers;
using NewCore.Function.ModuleRelayControl;

namespace NewCore.FunctionAdapters.ModuleRelayControl
{
  /// <summary>
  /// Адаптер для управления точками (реле) модуля МКР с отображением сообщений.
  /// </summary>
  internal class PointManagerAdapter : IPointManager
  {
    private readonly IRelaySwitchModule _moduleRelayControl;
    private readonly PointManager _pointManager;


    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PointManagerAdapter"/>.
    /// </summary>
    /// <param name="moduleRelayControl">Экземпляр модуля реле.</param>
    public PointManagerAdapter(IRelaySwitchModule moduleRelayControl)
    {
      _moduleRelayControl = moduleRelayControl ?? throw new ArgumentNullException(nameof(moduleRelayControl));
      _pointManager = new PointManager(moduleRelayControl);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectRelayAsync(BusPoint bus, int number, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _pointManager.ConnectRelayAsync(bus, number), userMessageService, deviceTask: true);
      var description = $"{number} к шине [{bus}]";

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _moduleRelayControl,
          $"Подключение точки {description}",
          result,
          1, userMessageService);
      }

      if (!result)
      {
        throw RelayExceptionFactory.ConnectPointFailed(description);
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectRelayAsync(BusPoint bus, int number, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _pointManager.DisconnectRelayAsync(bus, number), userMessageService, deviceTask: true);
      var description = $"{number} от шины [{bus}]";

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _moduleRelayControl,
          $"Отключение точки {description}",
          result,
          1, userMessageService);
      }

      if (!result)
      {
        throw RelayExceptionFactory.DisconnectPointFailed(description);
      }
      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _pointManager.ConnectRelayGroupAsync(bus, firstPoint, lastPoint), userMessageService, deviceTask: true);
      var description = $"{firstPoint}-{lastPoint} к шине [{bus}]";

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _moduleRelayControl,
          $"Подключение диапазона точек {description}",
          result,
          1, userMessageService);
      }

      if (!result)
        throw RelayExceptionFactory.ConnectRangeFailed(description);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _pointManager.DisconnectRelayGroupAsync(bus, firstPoint, lastPoint), userMessageService, deviceTask: true);
      var description = $"{firstPoint}-{lastPoint} от шины [{bus}]";

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _moduleRelayControl,
        $"Отключение диапазона точек {description}",
        result,
        1, userMessageService);
      }

      if (!result)
        throw RelayExceptionFactory.DisconnectRangeFailed(description);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectingAllPoint(IUserInteractionService? userMessageService = null)
    {
      var result = await _pointManager.DisconnectingAllPoint(userMessageService);
      var description = $"всех точек от всех шин";

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {

        await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _moduleRelayControl,
            $"Отключение {description}",
            result,
            1, userMessageService);
      }

      if (!result)
        throw RelayExceptionFactory.DisconnectRangeFailed(description);

      return result;
    }


    /// <inheritdoc />
    public async Task<string> CheckPoint(int numberPoint, IUserInteractionService? userMessageService = null)
    {
      // TODO : Обработка команды
      return await _pointManager.CheckPoint(numberPoint);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectingPointToNewBus(BusPoint bus, int nubmerPoint, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _pointManager.ConnectingPointToNewBus(bus, nubmerPoint), userMessageService, deviceTask: true);
      var description = $"{nubmerPoint} к шине [{bus}]";

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _moduleRelayControl,
        $"Переподключение точки {description}",
        result,
        1, userMessageService);
      }

      if (!result)
      {
        throw RelayExceptionFactory.ConnectingPointToNewBusFailed(description);
      }

      return result;
    }
  }
}
