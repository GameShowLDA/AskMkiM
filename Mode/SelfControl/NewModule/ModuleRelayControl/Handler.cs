using System.Windows;
using System.Windows.Media;
using DataBaseConfiguration.Services;
using Mode.SelfControl.Module.ModuleRelayControl;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;
using UI.Controls.Protocol;
using Utilities.Models;
using static AppConfiguration.Execution.ExecutionConfig;
using static Utilities.DelegateManager;
using static Utilities.LoggerUtility;
using static Utilities.Models.ShowMessageModel;

namespace Mode.SelfControl.NewModule.ModuleRelayControl
{
  /// <summary>
  /// Класс Handler реализует логику самоконтроля для устройств модуля реле. 
  /// Он подключается к устройствам, выполняет сброс системы, проверяет состояние реле, 
  /// отображает результаты проверки и обрабатывает ошибки, связанные с реле.
  /// </summary>
  internal class Handler
  {
    ProtocolUI ProtocolSelfCheckControl;
    private IRelaySwitchModule moduleRelayControl;

    /// <summary>
    /// Конструктор, принимающий объект ProtocolSelfCheckControl и модель устройства.
    /// </summary>
    /// <param name="protocolSelfCheck">Объект управления протоколом самоконтроля.</param>
    /// <param name="deviceModel">Модель устройства, используемая для создания объекта модуля реле.</param>
    internal Handler(ProtocolUI protocolSelfCheck, IRelaySwitchModule deviceModel)
    {
      ProtocolSelfCheckControl = protocolSelfCheck;
      moduleRelayControl = deviceModel;
    }

    #region StartDelegate

    /// <summary>
    /// Возвращает делегат, ссылающийся на метод RunSelfCheck, для запуска процесса самоконтроля.
    /// </summary>
    /// <returns>Делегат StartDelegate.</returns>
    internal StartDelegate GetStartDelegate()
    {
      StartDelegate startDelegate = RunSelfCheck;
      return startDelegate;
    }

    /// <summary>
    /// Асинхронный метод для выполнения настроек самоконтроля. Метод проверяет соединение с устройствами, 
    /// подключается к ним, выводит информационное сообщение, подключает счетчик, выполняет цикл проверки замыканий,
    /// а затем скрывает кнопку паузы и выводит итоговое сообщение.
    /// </summary>
    /// <param name="token">Токен отмены операции.</param>
    private async Task RunSelfCheck(CancellationToken token)
    {
      //if (!await GetIsIdleModeEnabled())
      //{
      //  var chassisNumber = moduleRelayControl.NumberChassis;
      //  var managerShassy = new ChassisManagerServices().GetById(chassisNumber);

      //  if (!await ProtocolSelfCheckControl.AttemptDeviceConnection(new List<IDevice>()
      //  {
      //    managerShassy,
      //    moduleRelayControl,
      //  }, ProtocolSelfCheckControl.ShowMessageAsync))
      //  {
      //    return;
      //  }
      //}

      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\r\nСамоконтроль МКР", SuccessMessage.TitleColor));

      await moduleRelayControl.MeterManager.ConnectMeterAsync();
      await PerformClosureCycle(token);

      ProtocolSelfCheckControl.PauseButtonVisibility = Visibility.Collapsed;

      ShowMessageModel showMessageModel = new ShowMessageModel()
      {
        Header = $"\r\nСамоконтроль",
        Message = $"[{SuccessMessage.Title}]",
        MessageColor = SuccessMessage.TitleColor,
        CanBeDeleted = false,
      };
      await ProtocolSelfCheckControl.ShowMessageAsync(showMessageModel);
    }

    /// <summary>
    /// Выполняет цикл замыканий точек и проверяет их состояние.
    /// Для каждой точки отправляется запрос, затем в зависимости от режима получаются данные 
    /// и формируется сообщение с результатом проверки.
    /// </summary>
    /// <param name="token">Токен отмены операции.</param>
    public async Task PerformClosureCycle(CancellationToken token)
    {
      for (int point = 1; point <= 350; point++)
      {
        ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();
        string answer = !await GetIsIdleModeEnabled()
          ? await moduleRelayControl.PointManager.CheckPoint(point)
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
            Header = $"\tТочка {point}",
            Message = model.SelfControl ? $"[{SuccessMessage.Title}]" : $"[{ErrorMessage.Title}]",
            MessageColor = model.SelfControl ? SuccessMessage.TitleColor : ErrorMessage.TitleColor,
            ExecutionError = !model.SelfControl,
          };
          showMessageModel.CanBeDeleted = !showMessageModel.ExecutionError;

          await ProtocolSelfCheckControl.ShowMessageAsync(showMessageModel);
          if (!model.SelfControl)
          {
            showMessageModel = new ShowMessageModel()
            {
              Header = $"\t\tПодключение точки",
              Message = model.ConnectPoint ? $"[{SuccessMessage.Title}]" : $"[{ErrorMessage.Title}]",
              MessageColor = model.ConnectPoint ? SuccessMessage.TitleColor : ErrorMessage.TitleColor,
              CanBeDeleted = model.ConnectPoint,
            };
            await ProtocolSelfCheckControl.ShowMessageAsync(showMessageModel);

            showMessageModel = new ShowMessageModel()
            {
              Header = $"\t\tПроверка реле на шине А",
              Message = model.DisconnectBusA ? $"[{SuccessMessage.Title}]" : $"[{ErrorMessage.Title}]",
              MessageColor = model.DisconnectBusA ? SuccessMessage.TitleColor : ErrorMessage.TitleColor,
              CanBeDeleted = model.DisconnectBusA,
            };
            await ProtocolSelfCheckControl.ShowMessageAsync(showMessageModel);

            showMessageModel = new ShowMessageModel()
            {
              Header = $"\t\tПроверка реле на шине B",
              Message = model.DisconnectBusB ? $"[{SuccessMessage.Title}]" : $"[{ErrorMessage.Title}]",
              MessageColor = model.DisconnectBusB ? SuccessMessage.TitleColor : ErrorMessage.TitleColor,
              CanBeDeleted = model.DisconnectBusB,
            };
            await ProtocolSelfCheckControl.ShowMessageAsync(showMessageModel);
          }
        }
        else
        {
          showMessageModel = new ShowMessageModel()
          {
            Header = $"\tОшибка данных!",
            HeaderColor = ErrorMessage.TitleColor,
            Message = answer,
          };
          await ProtocolSelfCheckControl.ShowMessageAsync(showMessageModel);
        }

        await Task.Delay(1, token);
      }
    }
    #endregion

    #region StopDelegate

    /// <summary>
    /// Возвращает делегат остановки самоконтроля, указывающий на метод StopAsync.
    /// </summary>
    /// <returns>Делегат StopDelegate.</returns>
    internal StopDelegate GetStopDelegate()
    {
      StopDelegate stopDelegate = StopAsync;
      return stopDelegate;
    }

    /// <summary>
    /// Останавливает самоконтроль, завершая процесс протокола и выводя итоговое сообщение.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task StopAsync(CancellationToken cancellationToken)
    {
      LogInformation("Запущен метод завершения самоконтроля");
      await ProtocolSelfCheckControl.FinalizeAsync();
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("\tСамоконтроль", null, $"[{SuccessMessage.Title}]", SuccessMessage.TitleColor));
      LogInformation("Завершён метод завершения самоконтроля");
    }
    #endregion
  }
}
