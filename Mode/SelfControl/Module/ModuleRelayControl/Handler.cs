using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Model;
using static Utilities.DelegateManager;
using System.Windows;
using Utilities.Models;
using static AppConfig.Config.ExecutionConfig;
using static AppConfig.Config.ProtocolConfig;
using static AppConfig.Config.LoopConfig;
using static AppConfig.Config.SystemStateManager;
using static AppConfig.EventAggregator;
using static AppConfig.SettingsFileReader;
using static Utilities.Models.ShowMessageModel;
using static Utilities.LoggerUtility;
using static AppConfig.Config.MeasurementErrorConfig;
using System.Windows.Media;
using UI.Controls.Protocol;
using Core.ConfigCollector;

namespace Mode.SelfControl.Module.ModuleRelayControl
{
  internal class Handler
  {
    ProtocolUI ProtocolSelfCheckControl;
    private readonly Tuple<string, Color> goodText = SuccessMessage;
    private readonly Tuple<string, Color> errorText = ErrorMessage;
    private Core.ModuleRelayControl.Model moduleRelayControl;

    internal Handler(ProtocolUI protocolSelfCheck, object deviceModel)
    {
      ProtocolSelfCheckControl = protocolSelfCheck;
      moduleRelayControl = Core.ModuleRelayControl.Model.CreateFromObject(deviceModel);
    }

    #region StartDelegate
    internal StartDelegate GetStartDelegate()
    {
      StartDelegate startDelegate = RunSelfCheck;
      return startDelegate;
    }

    /// <summary>
    /// Асинхронный метод для настроек самоконтроля.
    /// </summary>
    private async Task RunSelfCheck(CancellationToken token)
    {
      if (!await GetIsIdleModeEnabled())
      {
        var managerShassy = ConfigCollector.GetManagerShassy();

        if (!await ProtocolSelfCheckControl.AttemptDeviceConnection((new List<DeviceModel>()
        {
          managerShassy,
          moduleRelayControl,

        }), ProtocolSelfCheckControl.ShowMessageAsync))
        {
          return;
        }
      }

      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\r\nСамоконтроль МКР", goodText.Item2));

      await Core.ModuleRelayControl.Functions.ConnectMeterAsync(moduleRelayControl.IPAddress);
      await PerformClosureCycle(token);

      ProtocolSelfCheckControl.PauseButtonVisibility = Visibility.Collapsed;

      ShowMessageModel showMessageModel = new ShowMessageModel()
      {
        Header = $"\r\nСамоконтроль",
        Message = $"[{goodText.Item1}]",
        MessageColor = goodText.Item2,
        CanBeDeleted = false,
      };
      await ProtocolSelfCheckControl.ShowMessageAsync(showMessageModel);
    }

    /// <summary>
    /// Выполняет цикл замыканий точек с проверкой их состояния.
    /// </summary>
    /// <param name="token">Токен отмены операции.</param>
    public async Task PerformClosureCycle(CancellationToken token)
    {
      for (int point = 1; point <= 350; point++)
      {
        ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();
        string answer = !await GetIsIdleModeEnabled() ? await Core.ModuleRelayControl.Functions.CheckPoint(moduleRelayControl.IPAddress, point) : !await GetIsErrorSimulationEnabled() ? "104.1" : "104.2";

        SelfPointModel model;
        if (await GetIsIdleModeEnabled())
        {
          Random random = new Random();
          bool isErrorSimulation = await GetIsErrorSimulationEnabled();
          if (isErrorSimulation)
          {
            if (point % 10 == 0)
            {
              isErrorSimulation = true;
            }
            else
            {
              isErrorSimulation = false;
            }
          }

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
            Header = $"\tТочка {point}",
            Message = model.SelfControl ? $"[{goodText.Item1}]" : $"[{errorText.Item1}]",
            MessageColor = model.SelfControl ? goodText.Item2 : errorText.Item2,
            ExecutionError = model.SelfControl ? false : true,
          };
          showMessageModel.CanBeDeleted = !showMessageModel.ExecutionError;

          await ProtocolSelfCheckControl.ShowMessageAsync(showMessageModel);
          if (!model.SelfControl)
          {
            showMessageModel = new ShowMessageModel()
            {
              Header = $"\t\tПодключение точки",
              Message = model.ConnectPoint ? $"[{goodText.Item1}]" : $"[{errorText.Item1}]",
              MessageColor = model.ConnectPoint ? goodText.Item2 : errorText.Item2,
              CanBeDeleted = model.ConnectPoint ? true : false,
            };
            await ProtocolSelfCheckControl.ShowMessageAsync(showMessageModel);

            showMessageModel = new ShowMessageModel()
            {
              Header = $"\t\tПроверка реле на шине А",
              Message = model.DisconnectBusA ? $"[{goodText.Item1}]" : $"[{errorText.Item1}]",
              MessageColor = model.DisconnectBusA ? goodText.Item2 : errorText.Item2,
              CanBeDeleted = model.DisconnectBusA ? true : false,
            };
            await ProtocolSelfCheckControl.ShowMessageAsync(showMessageModel);

            showMessageModel = new ShowMessageModel()
            {
              Header = $"\t\tПроверка реле на шине B",
              Message = model.DisconnectBusB ? $"[{goodText.Item1}]" : $"[{errorText.Item1}]",
              MessageColor = model.DisconnectBusB ? goodText.Item2 : errorText.Item2,
              CanBeDeleted = model.DisconnectBusB ? true : false,
            };
            await ProtocolSelfCheckControl.ShowMessageAsync(showMessageModel);
          }
        }
        else
        {
          showMessageModel = new ShowMessageModel()
          {
            Header = $"\tОшибка данных!",
            HeaderColor = errorText.Item2,
            Message = answer,
          };
          await ProtocolSelfCheckControl.ShowMessageAsync(showMessageModel);
        }

        await Task.Delay(1, token);
      }
    }
    #endregion

    #region StopDelegate
    internal StopDelegate GetStopDelegate()
    {
      StopDelegate stopDelegate = StopAsync;
      return stopDelegate;
    }

    /// <summary>
    /// Останавливает самоконтроль, отключая необходимые компоненты и отображая соответствующие сообщения.
    /// </summary>
    private async Task StopAsync(CancellationToken cancellationToken)
    {
      LogInformation($"Запущен метод завершения самоконтроля");
      await ProtocolSelfCheckControl.FinalizeAsync();

      ShowMessageModel showMessageModel = new ShowMessageModel()
      {
        Header = "\tСамоконтроль",
        Message = $"[{goodText.Item1}]",
        MessageColor = goodText.Item2,
      };
      await ProtocolSelfCheckControl.ShowMessageAsync(showMessageModel);

      LogInformation($"Завершён метод завершения самоконтроля");
    }
    #endregion
  }
}
