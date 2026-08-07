using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
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
      await EquipmentMessages.PublishDeviceHealthCheckTitleAsync(_moduleRelay, userMessageService);

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
      await SelfTestMessages.PublishInformationAsync("Настройка устройств", userMessageService);
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

      await SelfTestMessages.PublishInformationAsync(testName, userMessageService);
      for (int point = 1; point <= _moduleRelay.PointCount; point++)
      {
        await UserActionHelper.RunWithUserRepeatAsync(() => CheckPoint(token, relaySwitchModule, point, settings, userMessageService), userMessageService);
      }
    }

    private async Task CheckBusesConnection(CancellationToken token, IRelaySwitchModule relaySwitchModule, ISwitchingDevice switchingDevice, ActionSettings settings, IUserInteractionService? userMessageService = null, string testNumber = null)
    {

      if (switchingDevice == null)
      {
        await SelfTestMessages.PublishResultAsync(
          "Устройство коммутации шин не задана в конфигурации!",
          false,
          userMessageService,
          skipPause: false);
        return;
      }

      await SelfTestMessages.PublishInformationAsync("Настройка устройств", userMessageService);

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
      if(!string.IsNullOrEmpty(testNumber))
      {
        testName = $"{testNumber}. {testName}";
      }

      await SelfTestMessages.PublishInformationAsync(testName, userMessageService);

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
      SelfBusModel busModel = SelfBusModel.FromJson(answer);
      if (busModel == null)
      {
        return (false, "Не удалось расшифровать овтет от устройства!");
      }

      if (busModel.ConnectMain && busModel.ConnectProtect)
      {
        return (true, string.Empty);
      }
      else
      {
        return (false, answer);
      }
    }

    private async Task<bool> CheckPoint(CancellationToken token, IRelaySwitchModule relaySwitchModule, int point, ActionSettings settings, IUserInteractionService? userMessageService = null)
    {
      token.ThrowIfCancellationRequested();

      string answer = await relaySwitchModule.PointManager.CheckPoint(point, userMessageService);
      SelfPointModel model = SelfPointModel.FromJson(answer);

      if (model != null)
      {
        model.SelfControl = model.ConnectPoint && model.DisconnectBusA && model.DisconnectBusB;
        string executionErrorMessage = model.SelfControl ? null : string.Empty;
        await SelfTestMessages.PublishResultAsync(
          $"Точка {point}",
          model.SelfControl,
          userMessageService,
          indentLevel: 1,
          executionErrorMessage: executionErrorMessage,
          executionError: !model.SelfControl,
          canBeDeleted: model.SelfControl);
      if (executionErrorMessage != null)
        {
          settings.DeviceResults[0].Tests.LastOrDefault()?.Errors.Add(new TestError
          {
            Message = $"Точка {point} - Ошибка подключения точки",
          });
        }
        if (!model.SelfControl)
        {
          var lastLine = userMessageService.GetLastLineNumber();
          userMessageService.AddError(ModuleRelayControlError.PointError(lastLine, $"{relaySwitchModule.NumberChassis}.{model.NumberDevice}.{model.NumberPoint}"));
          await SelfTestMessages.PublishResultAsync(
            "Подключение точки",
            model.ConnectPoint,
            userMessageService,
            indentLevel: 2,
            executionErrorMessage: model.ConnectPoint ? string.Empty : $"Точка[{point}] - Подключение точки",
            canBeDeleted: model.ConnectPoint);
          await SelfTestMessages.PublishResultAsync(
            "\t\tОтключение с шины А",
            model.DisconnectBusA,
            userMessageService,
            indentLevel: 2,
            executionErrorMessage: model.DisconnectBusA ? string.Empty : $"Точка[{point}] - Отключение с шины A",
            canBeDeleted: model.DisconnectBusA);
          await SelfTestMessages.PublishResultAsync(
            "\t\tОтключение с шины B",
            model.DisconnectBusB,
            userMessageService,
            indentLevel: 2,
            executionErrorMessage: model.DisconnectBusB ? string.Empty : $"Точка[{point}] - Отключение с шины B",
            canBeDeleted: model.DisconnectBusB);

          return false;
        }
      }
      else
      {
        await SelfTestMessages.PublishResultAsync(
          "\tОшибка данных!",
          false,
          userMessageService,
          message: answer);
        return false;
      }
      return true;
    }

    private async Task<bool> CheckBus(CancellationToken token, IRelaySwitchModule relaySwitchModule, int busNumber, ActionSettings settings, IUserInteractionService? userMessageService = null)
    {
      (bool, string) answer = await TryGetCheckBusConntcrion(busNumber);
      string name = $"Шины AB{busNumber}";
      await SelfTestMessages.PublishResultAsync(
        name,
        answer.Item1,
        userMessageService,
        indentLevel: 2,
        executionError: !answer.Item1,
        canBeDeleted: answer.Item1);
      if (!answer.Item1)
      {
        settings.DeviceResults[0].Tests.LastOrDefault()?.Errors.Add(new TestError
        {
          Message = $"{name} - Ошибка коммутации шин",
        });
      }

      if (!answer.Item1)
      {
        SelfBusModel selfBusModel = SelfBusModel.FromJson(answer.Item2);
        await SelfTestMessages.PublishResultAsync(
          $"\t\tПодключение защитных реле({selfBusModel.ProtectReleBusA},{selfBusModel.ProtectReleBusB})",
          selfBusModel.ConnectProtect,
          userMessageService,
          indentLevel: 3,
          canBeDeleted: selfBusModel.ConnectProtect);
        await SelfTestMessages.PublishResultAsync(
          $"\t\tПодключение основных реле({selfBusModel.MainReleBusA},{selfBusModel.MainReleBusB})",
          selfBusModel.ConnectMain,
          userMessageService,
          indentLevel: 3,
          canBeDeleted: selfBusModel.ConnectMain);

        return false;
      }
      return true;
    }
  }
}
