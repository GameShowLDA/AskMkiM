using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Device.Runtime.Commands;
using static Ask.Core.Shared.DTO.Protocol.ShowMessageModel;

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
    public async Task StartSelfCheck(CancellationToken cancellationToken, System.Enum typeConnector, IUserInteractionService? userMessageService = null, ISwitchingDevice device = null)
    {
      await userMessageService.ShowMessageAsync(EquipmentMessages.BuildDeviceHealthCheckTitle(_moduleRelay));

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
      await userMessageService.ShowMessageAsync(new ShowMessageModel("Настройка устройств"));
      if (!(await _moduleRelay.ConnectableManager.InitializeAsync(userMessageService)).Connect)
      {
        return;
      }

      await _moduleRelay.ConnectableManager.ResetAsync(userMessageService);
      await _moduleRelay.MeterManager.ConnectMeterAsync(
        ExecutionConfig.GetIsIdleModeEnabled() ? userMessageService : null);

      await userMessageService.ShowMessageAsync(new ShowMessageModel("Проверка подключения точек"));
      for (int point = 1; point <= _moduleRelay.PointCount; point++)
      {
        await UserActionHelper.RunWithUserRepeatAsync(() => CheckPoint(token, relaySwitchModule, point, userMessageService), userMessageService);
      }
    }

    private async Task CheckBusesConnection(CancellationToken token, IRelaySwitchModule relaySwitchModule, ISwitchingDevice switchingDevice, IUserInteractionService? userMessageService = null)
    {

      if (switchingDevice == null)
      {
        await userMessageService.ShowMessageAsync(new ShowMessageModel("Устройство коммутации шин не задана в конфигурации!", type: MessageType.Error));
        return;
      }

      await userMessageService.ShowMessageAsync(new ShowMessageModel("Настройка устройств"));

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

      await userMessageService.ShowMessageAsync(new ShowMessageModel("Проверка коммутации шин"));

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

    private async Task<bool> CheckPoint(CancellationToken token, IRelaySwitchModule relaySwitchModule, int point, IUserInteractionService? userMessageService = null)
    {
      token.ThrowIfCancellationRequested();

      string answer = await relaySwitchModule.PointManager.CheckPoint(point, userMessageService);
      SelfPointModel model = SelfPointModel.FromJson(answer);

      ShowMessageModel showMessageModel;
      if (model != null)
      {
        model.SelfControl = model.ConnectPoint && model.DisconnectBusA && model.DisconnectBusB;

        showMessageModel = new ShowMessageModel()
        {
          Header = $"Точка {point}",
          Status = model.SelfControl ? ShowMessageModel.MessageType.Success : MessageType.Error,
          ExecutionError = !model.SelfControl,
          ExecutionErrorMessage = model.SelfControl
            ? null
            : string.Empty,
          IndentLevel = 1,
        };
        showMessageModel.CanBeDeleted = !showMessageModel.ExecutionError;

        await userMessageService.ShowMessageAsync(showMessageModel, skipPause: true);

        if (!model.SelfControl)
        {
          var lastLine = userMessageService.GetLastLineNumber();
          userMessageService.AddError(ModuleRelayControlError.PointError(lastLine, $"{relaySwitchModule.NumberChassis}.{model.NumberDevice}.{model.NumberPoint}"));
          showMessageModel = new ShowMessageModel()
          {
            Header = $"Подключение точки",
            Status = model.ConnectPoint ? MessageType.Success : MessageType.Error,
            CanBeDeleted = model.ConnectPoint,
            ExecutionErrorMessage = model.ConnectPoint ? string.Empty : $"Точка[{point}] - Подключение точки",
            IndentLevel = 2,
          };
          await userMessageService.ShowMessageAsync(showMessageModel, skipPause: true);

          showMessageModel = new ShowMessageModel()
          {
            Header = $"\t\tОтключение с шины А",
            Status = model.DisconnectBusA ? MessageType.Success : MessageType.Error,
            CanBeDeleted = model.DisconnectBusA,
            ExecutionErrorMessage = model.DisconnectBusA ? string.Empty : $"Точка[{point}] - Отключение с шины A",
            IndentLevel = 2,
          };
          await userMessageService.ShowMessageAsync(showMessageModel, skipPause: true);

          showMessageModel = new ShowMessageModel()
          {
            Header = $"\t\tОтключение с шины B",
            Status = model.DisconnectBusB ? MessageType.Success : MessageType.Error,
            CanBeDeleted = model.DisconnectBusB,
            ExecutionErrorMessage = model.DisconnectBusB ? string.Empty : $"Точка[{point}] - Отключение с шины B",
            IndentLevel = 2,

          };
          await userMessageService.ShowMessageAsync(showMessageModel, skipPause: true);

          return false;
        }
      }
      else
      {
        showMessageModel = new ShowMessageModel()
        {
          Header = $"\tОшибка данных!",
          Status = MessageType.Error,
          Message = answer,
        };
        await userMessageService.ShowMessageAsync(showMessageModel, skipPause: true);
        return false;
      }
      return true;
    }

    private async Task<bool> CheckBus(CancellationToken token, IRelaySwitchModule relaySwitchModule, int busNumber, IUserInteractionService? userMessageService = null)
    {
      (bool, string) answer = await TryGetCheckBusConntcrion(busNumber);

      ShowMessageModel showMessageModel;
      showMessageModel = new ShowMessageModel()
      {
        Header = $"Шины AB{busNumber}",
        Status = answer.Item1 ? ShowMessageModel.MessageType.Success : MessageType.Error,
        ExecutionError = !answer.Item1,
        IndentLevel = 2,
      };
      showMessageModel.CanBeDeleted = !showMessageModel.ExecutionError;
      await userMessageService.ShowMessageAsync(showMessageModel, skipPause: true);

      if (!answer.Item1)
      {
        SelfBusModel selfBusModel = SelfBusModel.FromJson(answer.Item2);
        showMessageModel = new ShowMessageModel()
        {
          Header = $"\t\tПодключение защитных реле({selfBusModel.ProtectReleBusA},{selfBusModel.ProtectReleBusB})",
          Status = selfBusModel.ConnectProtect ? MessageType.Success : MessageType.Error,
          CanBeDeleted = selfBusModel.ConnectProtect,
          IndentLevel = 3,
        };
        await userMessageService.ShowMessageAsync(showMessageModel, skipPause: true);

        showMessageModel = new ShowMessageModel()
        {
          Header = $"\t\tПодключение основных реле({selfBusModel.MainReleBusA},{selfBusModel.MainReleBusB})",
          Status = selfBusModel.ConnectMain ? MessageType.Success : MessageType.Error,
          CanBeDeleted = selfBusModel.ConnectMain,
          IndentLevel = 3,
        };
        await userMessageService.ShowMessageAsync(showMessageModel, skipPause: true);

        return false;
      }
      return true;
    }
  }
}
