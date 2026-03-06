using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
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
      var description = $"{number} к шине [{bus}]";
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _pointManager.ConnectRelayAsync(bus, number);

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_moduleRelayControl, $"Подключение точки {description}", succes, 1, userMessageService);
        }
        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw RelayExceptionFactory.ConnectPointFailed(description);
      }
      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectRelayAsync(BusPoint bus, int number, IUserInteractionService? userMessageService = null)
    {
      var description = $"{number} от шины [{bus}]";
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _pointManager.DisconnectRelayAsync(bus, number);

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_moduleRelayControl, $"Отключение точки {description}", succes, 1, userMessageService);
        }
        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw RelayExceptionFactory.DisconnectPointFailed(description);
      }
      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint, IUserInteractionService? userMessageService = null)
    {
      var description = $"{firstPoint}-{lastPoint} к шине [{bus}]";

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _pointManager.ConnectRelayGroupAsync(bus, firstPoint, lastPoint);
        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_moduleRelayControl, $"Подключение диапазона точек {description}", succes, 1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw RelayExceptionFactory.ConnectRangeFailed(description);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint, IUserInteractionService? userMessageService = null)
    {
      var description = $"{firstPoint}-{lastPoint} от шины [{bus}]";

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _pointManager.DisconnectRelayGroupAsync(bus, firstPoint, lastPoint);

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_moduleRelayControl, $"Отключение диапазона точек {description}", succes, 1, userMessageService);
        }
        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw RelayExceptionFactory.DisconnectRangeFailed(description);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectingAllPoint(IUserInteractionService? userMessageService = null)
    {
      var description = $"всех точек от всех шин";
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _pointManager.DisconnectingAllPoint(userMessageService);

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_moduleRelayControl, $"Отключение {description}", succes, 1, userMessageService);
        }
        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw RelayExceptionFactory.DisconnectRangeFailed(description);

      return result;
    }


    public async Task<bool> DisconnectingAllPointFromBusA(IUserInteractionService? userMessageService = null)
    {
      var description = $"всех точек от шины А";
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _pointManager.DisconnectingAllPointFromBusA(userMessageService);

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_moduleRelayControl, $"Отключение {description}", succes, 1, userMessageService);
        }
        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw RelayExceptionFactory.DisconnectRangeFailed(description);

      return result;
    }

    public async Task<bool> DisconnectingAllPointFromBusB(IUserInteractionService? userMessageService = null)
    {
      var description = $"всех точек от шины В";
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _pointManager.DisconnectingAllPointFromBusB(userMessageService);

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_moduleRelayControl, $"Отключение {description}", succes, 1, userMessageService);
        }
        return succes;
      }, userMessageService, deviceTask: true);

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
      var description = $"{nubmerPoint} к шине [{bus}]";

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _pointManager.ConnectingPointToNewBus(bus, nubmerPoint);

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _moduleRelayControl,
          $"Переподключение точки {description}",
          succes,
          1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw RelayExceptionFactory.ConnectingPointToNewBusFailed(description);
      }

      return result;
    }

    public IReadOnlyList<PointConnectionInfo> GetConnectedPoints() => _pointManager.GetConnectedPoints();
  }
}
