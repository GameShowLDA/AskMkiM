using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Device.Runtime.AskMkiM.Base.Commands;
using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing;

namespace Ask.Device.Runtime.AskMkiM.Function.ModuleRelayControl.SelfCheck
{
  public class SelfTestManager : ISelfTestCheckerModuleRelayControl
  {
    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly IRelaySwitchModule _moduleRelay;
    private readonly ModuleRelayControlQueryExecutor _queryExecutor;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BusManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public SelfTestManager(IRelaySwitchModule moduleRelay)
    {
      _moduleRelay = moduleRelay;
      _queryExecutor = new ModuleRelayControlQueryExecutor(moduleRelay);
    }
    public Type GetTestTypeEnum()
    {
      return typeof(RelaySwitchTypeConnector);
    }

    /// <inheritdoc />
    public async Task StartSelfCheck(CancellationToken cancellationToken, Enum typeConnector, IUserInteractionService? userMessageService = null, ISwitchingDevice device = null)
    {
      await ModuleRelayControlResponseProcessor.PublishSelfTestTitleAsync(_moduleRelay, userMessageService);

      switch (typeConnector)
      {
        case RelaySwitchTypeConnector.Points:
          await PerformClosureCycle(cancellationToken, _moduleRelay, userMessageService);
          break;

        case RelaySwitchTypeConnector.BusCommutation:
          await CheckBusesConnection(cancellationToken, _moduleRelay, device, userMessageService);
          break;

        case RelaySwitchTypeConnector.FullCheck:
          await PerformClosureCycle(cancellationToken, _moduleRelay, userMessageService);
          await CheckBusesConnection(cancellationToken, _moduleRelay, device, userMessageService);
          break;
      }
      await _moduleRelay.ConnectableManager.ResetAsync(userMessageService);
    }

    /// <summary>
    /// Выполняет цикл замыканий точек и проверяет их состояние.
    /// Для каждой точки отправляется запрос, затем в зависимости от режима получаются данные 
    /// и формируется сообщение с результатом проверки.
    /// </summary>
    /// <param name="token">Токен отмены операции.</param>
    private async Task PerformClosureCycle(CancellationToken token, IRelaySwitchModule relaySwitchModule, IUserInteractionService? userMessageService = null)
    {
      await ModuleRelayControlResponseProcessor.PublishSelfTestInformationAsync("Настройка устройств", userMessageService);
      if (!(await _moduleRelay.ConnectableManager.InitializeAsync(userMessageService)).Connect)
      {
        return;
      }

      await _moduleRelay.ConnectableManager.ResetAsync(userMessageService);
      await _moduleRelay.MeterManager.ConnectMeterAsync(userMessageService);

      await ModuleRelayControlResponseProcessor.PublishSelfTestInformationAsync("Проверка подключения точек", userMessageService);
      for (int point = 1; point <= _moduleRelay.PointCount; point++)
      {
        await UserActionHelper.RunWithUserRepeatAsync(() => CheckPoint(token, relaySwitchModule, point, userMessageService), userMessageService);
      }
    }

    private async Task CheckBusesConnection(CancellationToken token, IRelaySwitchModule relaySwitchModule, ISwitchingDevice switchingDevice, IUserInteractionService? userMessageService = null)
    {

      if (switchingDevice == null)
      {
        await ModuleRelayControlResponseProcessor.PublishSelfTestResultAsync(
          "Устройство коммутации шин не задана в конфигурации!",
          false,
          userMessageService,
          skipPause: false);
        return;
      }

      await ModuleRelayControlResponseProcessor.PublishSelfTestInformationAsync("Настройка устройств", userMessageService);

      if (!(await switchingDevice.ConnectableManager.InitializeAsync(userMessageService)).Connect || !(await _moduleRelay.ConnectableManager.InitializeAsync(userMessageService)).Connect)
      {
        return;
      }

      await _moduleRelay.ConnectableManager.ResetAsync(userMessageService);

      await switchingDevice.ConnectableManager.ResetAsync(userMessageService);
      if (!await switchingDevice.ConnectorManager.ConnectAllBuses(userMessageService))
      {
        return;
      }

      await ModuleRelayControlResponseProcessor.PublishSelfTestInformationAsync("Проверка коммутации шин", userMessageService);

      for (int busNumber = 1; busNumber <= 4; busNumber++)
      {
        await UserActionHelper.RunWithUserRepeatAsync(
          () => CheckBus(token, relaySwitchModule, busNumber, userMessageService),
          userMessageService,
          deviceTask: ExecutionConfig.GetIsIdleModeEnabled());
      }
    }

    public async Task<(bool, string)> TryGetCheckBusConntcrion(int number, IUserInteractionService? userMessageService = null)
    {
      DeviceCommand cmd = new DeviceCommand(10, number);
      string answer = await _queryExecutor.QueryAsync(cmd.ToString(), timeout: 1000);
      bool success = ModuleRelayControlResponseProcessor.CheckExternalBusSelfTest(
        answer,
        _moduleRelay,
        number);
      return (success, success ? string.Empty : answer);
    }

    private async Task<bool> CheckPoint(CancellationToken token, IRelaySwitchModule relaySwitchModule, int point, IUserInteractionService? userMessageService = null)
    {
      token.ThrowIfCancellationRequested();

      string answer = await relaySwitchModule.PointManager.CheckPoint(point, userMessageService);
      return await ModuleRelayControlResponseProcessor.CheckPointSelfTestAsync(
        answer,
        relaySwitchModule,
        point,
        userMessageService);
    }

    private async Task<bool> CheckBus(CancellationToken token, IRelaySwitchModule relaySwitchModule, int busNumber, IUserInteractionService? userMessageService = null)
    {
      token.ThrowIfCancellationRequested();

      DeviceCommand command = new(10, busNumber);
      string response = await _queryExecutor.QueryAsync(command.ToString(), timeout: 1000);
      return await ModuleRelayControlResponseProcessor.CheckExternalBusSelfTestAsync(
        response,
        relaySwitchModule,
        busNumber,
        userMessageService);
    }
  }
}


