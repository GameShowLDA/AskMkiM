using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Interface;
using NewCore.Base.Device;
using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Additionally;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using NewCore.Device;
using Newtonsoft.Json.Linq;
using Utilities;
using Utilities.Interface;
using Utilities.Models;
using YamlDotNet.Core.Tokens;
using static AppConfiguration.Execution.ExecutionConfig;
using static Utilities.DelegateManager;
using static Utilities.LoggerUtility;
using static Utilities.Models.ShowMessageModel;

namespace NewCore.Function.ModuleRelayControl.SelfCheck
{
  public class SelfTestManager : ISelfTestCheckerModuleRelayControl
  {
    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly Device.ModuleRelayControl _moduleRelay;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BusManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public SelfTestManager(Device.ModuleRelayControl moduleRelay) => _moduleRelay = moduleRelay;
    public Type GetTestTypeEnum()
    {
      return typeof(TypeConnector);
    }

    /// <inheritdoc />
    public async Task StartSelfCheck(CancellationToken cancellationToken, System.Enum typeConnector, IUserMessageService messageService, ISwitchingDevice device = null)
    {
      switch (typeConnector)
      {
        case TypeConnector.Points:
          await PerformClosureCycle(cancellationToken, messageService, _moduleRelay);
          break;

        case TypeConnector.BusCommutation:
          await CheckBusesConnection(cancellationToken, messageService, _moduleRelay, device);
          break;

        case TypeConnector.FullCheck:
          await PerformClosureCycle(cancellationToken, messageService, _moduleRelay);
          await CheckBusesConnection(cancellationToken, messageService, _moduleRelay, device);
          break;
      }


    }

    /// <summary>
    /// Выполняет цикл замыканий точек и проверяет их состояние.
    /// Для каждой точки отправляется запрос, затем в зависимости от режима получаются данные 
    /// и формируется сообщение с результатом проверки.
    /// </summary>
    /// <param name="token">Токен отмены операции.</param>
    private async Task PerformClosureCycle(CancellationToken token, IUserMessageService messageService, IRelaySwitchModule relaySwitchModule)
    {
      await messageService.ShowMessageAsync(new ShowMessageModel("Настройка устройств"));
      if (!(await _moduleRelay.ConnectableManager.InitializeAsync(messageService)).Connect)
      {
        return;
      }
      await _moduleRelay.MeterManager.ConnectMeterAsync();

      await messageService.ShowMessageAsync(new ShowMessageModel("Проверка подключения точек"));
      for (int point = 1; point <= 350; point++)
      {
        await UserActionHelper.RunWithUserRepeatAsync(() => CheckPoint(token, messageService, relaySwitchModule, point), messageService);
      }
    }

    private async Task CheckBusesConnection(CancellationToken token, IUserMessageService messageService, IRelaySwitchModule relaySwitchModule, ISwitchingDevice switchingDevice)
    {
      await messageService.ShowMessageAsync(new ShowMessageModel("Настройка устройств"));
      if (!(await switchingDevice.ConnectableManager.InitializeAsync(messageService)).Connect || !(await _moduleRelay.ConnectableManager.InitializeAsync(messageService)).Connect)
      {
        return;
      }

      await switchingDevice.ConnectableManager.ResetAsync(messageService);
      if (!await switchingDevice.ConnectorManager.ConnectAllBuses())
      {
        return;
      }

      await messageService.ShowMessageAsync(new ShowMessageModel("Проверка коммутации шин"));

      for (int busNumber = 1; busNumber <= 4; busNumber++)
      {
        await UserActionHelper.RunWithUserRepeatAsync(() => CheckBus(token, messageService, relaySwitchModule, busNumber), messageService);
      }
    }

    public async Task<(bool, string)> TryGetCheckBusConntcrion(int number)
    {
      if (await GetIsIdleModeEnabled())
      {
        return (true, string.Empty);
      }

      DeviceCommand cmd = new DeviceCommand(10, number);
      string answer = await _moduleRelay.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 1000);
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

    private async Task<bool> CheckPoint(CancellationToken token, IUserMessageService messageService, IRelaySwitchModule relaySwitchModule, int point)
    {
      token.ThrowIfCancellationRequested();

      string answer = !await GetIsIdleModeEnabled()
        ? await relaySwitchModule.PointManager.CheckPoint(point)
        : !await GetIsErrorSimulationEnabled() ? "104.1" : "104.2";

      SelfPointModel model;
      if (await GetIsIdleModeEnabled())
      {
        Random random = new Random();
        bool isErrorSimulation = await GetIsErrorSimulationEnabled();
        isErrorSimulation = isErrorSimulation && point % 10 == 0;
        model = new SelfPointModel
        {
          DisconnectBusB = isErrorSimulation ? random.Next(2) == 1 : true,
          DisconnectBusA = isErrorSimulation ? random.Next(2) == 1 : true,
          ConnectPoint = isErrorSimulation ? random.Next(2) == 1 : true,
        };
        model.SelfControl = model.DisconnectBusB && model.DisconnectBusA && model.ConnectPoint;
      }
      else
      {
        model = SelfPointModel.FromJson(answer);
      }

      ShowMessageModel showMessageModel;
      if (model != null)
      {
        showMessageModel = new ShowMessageModel()
        {
          Header = $"Точка {point}",
          Status = model.SelfControl ? ShowMessageModel.MessageType.Success : MessageType.Error,
          ExecutionError = !model.SelfControl,
          IndentLevel = 1,
        };
        showMessageModel.CanBeDeleted = !showMessageModel.ExecutionError;

        await messageService.ShowMessageAsync(showMessageModel, skipPause: true);

        model.SelfControl = model.ConnectPoint && model.DisconnectBusA && model.DisconnectBusB;
        if (!model.SelfControl)
        {
          var lastLine = messageService.GetLastLineNumberAsync();
          messageService.AddError(AppConfiguration.Error.Device.ModuleRelayControl.ModuleRelayControlError.PointError(lastLine, $"{relaySwitchModule.NumberChassis}.{model.NumberDevice}.{model.NumberPoint}"));
          showMessageModel = new ShowMessageModel()
          {
            Header = $"Подключение точки",
            Status = model.ConnectPoint ? MessageType.Success : MessageType.Error,
            CanBeDeleted = model.ConnectPoint,
            IndentLevel = 2,
          };
          await messageService.ShowMessageAsync(showMessageModel, skipPause: true);

          showMessageModel = new ShowMessageModel()
          {
            Header = $"\t\tПроверка реле на шине А",
            Status = model.DisconnectBusA ? MessageType.Success : MessageType.Error,
            CanBeDeleted = model.DisconnectBusA,
            IndentLevel = 2,
          };
          await messageService.ShowMessageAsync(showMessageModel, skipPause: true);

          showMessageModel = new ShowMessageModel()
          {
            Header = $"\t\tПроверка реле на шине B",
            Status = model.DisconnectBusB ? MessageType.Success : MessageType.Error,
            CanBeDeleted = model.DisconnectBusB,
            IndentLevel = 2,

          };
          await messageService.ShowMessageAsync(showMessageModel, skipPause: true);

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
        await messageService.ShowMessageAsync(showMessageModel, skipPause: true);
        return false;
      }
      return true;
    }

    private async Task<bool> CheckBus(CancellationToken token, IUserMessageService messageService, IRelaySwitchModule relaySwitchModule, int busNumber)
    {
      (bool, string) answer = !await GetIsIdleModeEnabled() ? await TryGetCheckBusConntcrion(busNumber) : (true, string.Empty);

      ShowMessageModel showMessageModel;
      showMessageModel = new ShowMessageModel()
      {
        Header = $"Шины AB{busNumber}",
        Status = answer.Item1 ? ShowMessageModel.MessageType.Success : MessageType.Error,
        ExecutionError = !answer.Item1,
        IndentLevel = 2,
      };
      showMessageModel.CanBeDeleted = !showMessageModel.ExecutionError;
      await messageService.ShowMessageAsync(showMessageModel, skipPause: true);

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
        await messageService.ShowMessageAsync(showMessageModel, skipPause: true);

        showMessageModel = new ShowMessageModel()
        {
          Header = $"\t\tПодключение основных реле({selfBusModel.MainReleBusA},{selfBusModel.MainReleBusB})",
          Status = selfBusModel.ConnectMain ? MessageType.Success : MessageType.Error,
          CanBeDeleted = selfBusModel.ConnectMain,
          IndentLevel = 3,
        };
        await messageService.ShowMessageAsync(showMessageModel, skipPause: true);

        return false;
      }
      return true;
    }
  }
}
