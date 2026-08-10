using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing;
using Ask.Device.Runtime.Commands;
using YamlDotNet.Serialization;

namespace Ask.Device.Runtime.Function.ModuleRelayControl.SelfCheck
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
    public async Task StartSelfCheck(CancellationToken cancellationToken, System.Enum typeConnector, ActionSettings settings, IUserInteractionService? userMessageService = null, ISwitchingDevice device = null)
    {
      settings.DeviceResults.Add(new DeviceExecutionResult(_moduleRelay.Name, _moduleRelay.NumberChassis, _moduleRelay.Number));
      await ModuleRelayControlResponseProcessor.PublishSelfTestTitleAsync(_moduleRelay, userMessageService);

      switch (typeConnector)
      {
        case RelaySwitchTypeConnector.Points:
          await PerformClosureCycle(cancellationToken, _moduleRelay, settings, userMessageService);
          break;

        case RelaySwitchTypeConnector.BusCommutation:
          await CheckBusesConnection(cancellationToken, _moduleRelay, device, settings, userMessageService);
          break;

        case RelaySwitchTypeConnector.FullCheck:
          await PerformClosureCycle(cancellationToken, _moduleRelay, settings, userMessageService, 1.ToString());
          await CheckBusesConnection(cancellationToken, _moduleRelay, device, settings, userMessageService, 2.ToString());
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
    private async Task PerformClosureCycle(CancellationToken token, IRelaySwitchModule relaySwitchModule, ActionSettings settings, IUserInteractionService? userMessageService = null, string testNumber = null)
    {
      await ModuleRelayControlResponseProcessor.PublishSelfTestInformationAsync("Настройка устройств", userMessageService);
      if (!(await _moduleRelay.ConnectableManager.InitializeAsync(userMessageService)).Connect)
      {
        return;
      }

      await _moduleRelay.ConnectableManager.ResetAsync(userMessageService);
      await _moduleRelay.MeterManager.ConnectMeterAsync(
        ExecutionConfig.GetIsIdleModeEnabled() ? userMessageService : null);
      var testName = "Тест подключения точек";
      settings.DeviceResults.LastOrDefault()?.Tests.Add(new TestExecutionResult
      {
        TestName = testName,
      });
      if (!string.IsNullOrEmpty(testNumber))
      {
        testName = $"{testNumber}. {testName}";
      }

      await ModuleRelayControlResponseProcessor.PublishSelfTestInformationAsync(testName, userMessageService);
      for (int point = 1; point <= _moduleRelay.PointCount; point++)
      {
        await UserActionHelper.RunWithUserRepeatAsync(() => CheckPoint(token, relaySwitchModule, point, settings, userMessageService), userMessageService);
      }
    }

    private async Task CheckBusesConnection(CancellationToken token, IRelaySwitchModule relaySwitchModule, ISwitchingDevice switchingDevice, ActionSettings settings, IUserInteractionService? userMessageService = null, string testNumber = null)
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
      var testName = "Тест коммутации шин";
      settings.DeviceResults.LastOrDefault()?.Tests.Add(new TestExecutionResult
      {
        TestName = testName,
      });
      if (!string.IsNullOrEmpty(testNumber))
      {
        testName = $"{testNumber}. {testName}";
      }

      await ModuleRelayControlResponseProcessor.PublishSelfTestInformationAsync(testName, userMessageService);

      for (int busNumber = 1; busNumber <= 4; busNumber++)
      {
        await UserActionHelper.RunWithUserRepeatAsync(
          () => CheckBus(token, relaySwitchModule, busNumber, settings, userMessageService),
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

    private async Task<bool> CheckPoint(CancellationToken token, IRelaySwitchModule relaySwitchModule, int point, ActionSettings settings, IUserInteractionService? userMessageService = null)
    {
      token.ThrowIfCancellationRequested();

      string answer = await relaySwitchModule.PointManager.CheckPoint(point, userMessageService);
      bool success = await ModuleRelayControlResponseProcessor.CheckPointSelfTestAsync(
        answer,
        relaySwitchModule,
        point,
        userMessageService);
      if (!success)
      {
        settings.DeviceResults[0].Tests.LastOrDefault()?.Errors.Add(new TestError
        {
          Message = $"Точка {point} - Ошибка подключения точки",
        });
      }

      return success;
    }

    private async Task<bool> CheckBus(CancellationToken token, IRelaySwitchModule relaySwitchModule, int busNumber, ActionSettings settings, IUserInteractionService? userMessageService = null)
    {
      string name = $"Шины AB{busNumber}";
      token.ThrowIfCancellationRequested();

      DeviceCommand command = new(10, busNumber);
      string response = await _queryExecutor.QueryAsync(command.ToString(), timeout: 1000);
      bool success = await ModuleRelayControlResponseProcessor.CheckExternalBusSelfTestAsync(
        response,
        relaySwitchModule,
        busNumber,
        userMessageService);
      if (!success)
      {
        settings.DeviceResults[0].Tests.LastOrDefault()?.Errors.Add(new TestError
        {
          Message = $"{name} - Ошибка коммутации шин",
        });
      }

      return success;
    }
  }
}
