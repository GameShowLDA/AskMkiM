using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Function.ModuleRelayControl;

namespace Ask.Device.Application.FunctionAdapters.ModuleRelayControl
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
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(
        () => _pointManager.ConnectRelayAsync(bus, number, userMessageService),
        userMessageService,
        deviceTask: true);

      if (!result)
      {
        var description = $"{number} к шине [{bus}]";
        throw RelayExceptionFactory.ConnectPointFailed(description);
      }
      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectRelayAsync(BusPoint bus, int number, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(
        () => _pointManager.DisconnectRelayAsync(bus, number, userMessageService),
        userMessageService,
        deviceTask: true);

      if (!result)
      {
        var description = $"{number} от шины [{bus}]";
        throw RelayExceptionFactory.DisconnectPointFailed(description);
      }
      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectRelayVerifiedAsync(BusPoint bus, int number, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(
        () => _pointManager.ConnectRelayVerifiedAsync(bus, number, userMessageService),
        userMessageService,
        deviceTask: true);

      if (!result)
      {
        var description = $"{number} к шине [{bus}]";
        throw RelayExceptionFactory.ConnectPointFailed(description);
      }
      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectRelayVerifiedAsync(BusPoint bus, int number, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(
        () => _pointManager.DisconnectRelayVerifiedAsync(bus, number, userMessageService),
        userMessageService,
        deviceTask: true);

      if (!result)
      {
        var description = $"{number} от шины [{bus}]";
        throw RelayExceptionFactory.DisconnectPointFailed(description);
      }
      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint, IUserInteractionService? userMessageService = null)
    {

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        return await _pointManager.ConnectRelayGroupAsync(bus, firstPoint, lastPoint, userMessageService);
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        var description = $"{firstPoint}-{lastPoint} к шине [{bus}]";
        throw RelayExceptionFactory.ConnectRangeFailed(description);
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint, IUserInteractionService? userMessageService = null)
    {

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        return await _pointManager.DisconnectRelayGroupAsync(bus, firstPoint, lastPoint, userMessageService);
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        var description = $"{firstPoint}-{lastPoint} от шины [{bus}]";
        throw RelayExceptionFactory.DisconnectRangeFailed(description);
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectingAllPoint(IUserInteractionService? userMessageService = null)
    {
      var description = $"всех точек от всех шин";
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _pointManager.DisconnectingAllPoint(userMessageService);

        await Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing.ModuleRelayControlResponseProcessor
          .PublishOperationResultAsync(_moduleRelayControl, $"Отключение {description}", succes, userMessageService);
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

        await Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing.ModuleRelayControlResponseProcessor
          .PublishOperationResultAsync(_moduleRelayControl, $"Отключение {description}", succes, userMessageService);
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

        await Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing.ModuleRelayControlResponseProcessor
          .PublishOperationResultAsync(_moduleRelayControl, $"Отключение {description}", succes, userMessageService);
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
        return await _pointManager.ConnectingPointToNewBus(bus, nubmerPoint, userMessageService);
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
