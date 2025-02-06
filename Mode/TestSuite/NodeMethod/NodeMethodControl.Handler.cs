using Core.Communication;
using Core.ConfigCollector;
using Core.Model;
using Mode.Base.SearchDevices;
using Utilities.Models;
using static AppConfig.Config.ExecutionConfig;
using static AppConfig.Config.SystemStateManager;
using static Core.ModuleRelayControl.Enums;

namespace Mode.TestSuite.NodeMethod
{
  partial class NodeMethodControl
  {
    private bool completed;

    private double electricalParameter;
    private int time;

    /// <summary>
    /// Получает или устанавливает значение, указывающее, завершено ли измерение.
    /// При установке значения обновляет состояние доступности полей измерения.
    /// </summary>
    private bool Completed
    {
      get => completed;
      set
      {
        testDataModel.FirstPointData.IsReadOnly = value;
        testDataModel.LastPointData.IsReadOnly = value;
        completed = value;
      }
    }

    private async Task Stop(CancellationToken token)
    {
      gptLibrary.Disconnect();
      await CommunicationManager.ResetAllSystem();
    }

    /// <summary>
    /// Главный метод, который управляет процессом теста методом узла.
    /// </summary>
    public async Task ExecuteTestProcess(CancellationToken token)
    {
      if (!await GetIsIdleModeEnabled())
      {
        if (!await GetIsActivePower())
        {
          await ProtocolSelfCheckControl.AbortExecution();
        }
      }

      await CommunicationManager.ResetAllSystem();
      Completed = true;
      if (!await ValidateAndConnectDevice(token))
      {
        await ProtocolSelfCheckControl.AbortExecution();
      }

      BusPoint bus = BusPoint.A;
      if (IsBusAActive)
      {
        bus = BusPoint.B;
      }
      var mkr = BlockNumberGenerator.GetBlockModelsBetween(testDataModel.FirstModuleRelayControl, testDataModel.LastModuleRelayControl);

      await ConnectPointsToOppositeBusAsync(mkr, bus);
      await ConnectAndTestPointsAsync(mkr, bus);

      Completed = false;
      await CommunicationManager.ResetAllSystem();
    }

    /// <summary>
    /// Проверка данных и подключение к устройствам.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<bool> ValidateAndConnectDevice(CancellationToken token)
    {
      InputValidator inputValidator = new InputValidator();
      try
      {
        token.ThrowIfCancellationRequested();

        var firstPoint = await inputValidator.ValidateMeasurementPointAsync(ShowMessageAsync, testDataModel.FirstPointData.Text);
        var secondPoint = await inputValidator.ValidateMeasurementPointAsync(ShowMessageAsync, testDataModel.LastPointData.Text);

        if (!firstPoint.IsSuccess || !secondPoint.IsSuccess)
        {
          return false;
        }
        if (!inputValidator.ValidateUniqueMeasurementPointAsync(firstPoint.PointModel, secondPoint.PointModel))
        {
          return false;
        }


        List<DeviceModel> deviceModels = new List<DeviceModel>();

        testDataModel.FirstPointModel = firstPoint.PointModel;
        testDataModel.LastPointModel = secondPoint.PointModel;

        testDataModel.ManagerShassy = firstPoint.ManagerShassy;
        testDataModel.FirstModuleRelayControl = firstPoint.ModuleRelayControl;
        testDataModel.LastModuleRelayControl = secondPoint.ModuleRelayControl;
        deviceBusCommutation = ConfigCollector.GetDeviceBusCommutation();
        gptLibrary = Core.GptLibrary.Model.CreateAsync();
        deviceModels.Add(testDataModel.ManagerShassy);
        deviceModels.Add(deviceBusCommutation);
        deviceModels.Add(gptLibrary);

        var mkr = BlockNumberGenerator.GetBlockModelsBetween(testDataModel.FirstModuleRelayControl, testDataModel.LastModuleRelayControl);
        foreach (var deviceModel in mkr)
        {
          deviceModels.Add(deviceModel);
        }

        if (!await AttemptDeviceConnection(deviceModels))
        {
          return false;
        }

      }
      catch (InvalidOperationException)
      {
        Completed = false;
        return false;
      }

      token.ThrowIfCancellationRequested();
      if (await GetIsStepByStepModeEnabled())
      {
        ProtocolSelfCheckControl.ShowOnlyStopAndFinishButtons();
      }

      if (!double.TryParse(testDataModel.ElectricParameterData.Text, out electricalParameter))
      {
        electricalParameter = 2000.0;
      }

      if (!int.TryParse(TimeData.Text, out time))
      {
        time = 1;
      }

      return true;
    }

    /// <summary>
    /// Подключает все точки к противоположной шине.
    /// </summary>
    /// <param name="mkr">Список модулей МКР.</param>
    /// <param name="bus">Тип шины, к которой подключаются точки.</param>
    /// <param name="goodText">Текст для отображения в сообщениях.</param>
    public async Task ConnectPointsToOppositeBusAsync(List<Core.ModuleRelayControl.Model> mkr, BusPoint bus)
    {

      for (int i = 0; i < mkr.Count; i++)
      {
        await Core.ModuleRelayControl.Functions.ConnectBusAsync(mkr[i].IPAddress, BusModuleRelayControl.AB1, true);
        ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();
        await ShowMessageAsync(new ShowMessageModel("Точки МКР номер", null, (i + 1).ToString(), goodText.Item2));

        var points = GetPoints(mkr[i], i + 1);
        if (!await GetIsIdleModeEnabled())
        {
          await Core.ModuleRelayControl.Functions.ConnectRelayGroupAsync(mkr[i].IPAddress, bus, points[0], points[points.Count - 1]);
          await Task.Delay(1500);
        }

        foreach (var point in points)
        {
          ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();
          await ShowMessageAsync(new ShowMessageModel($"\tПодключаем к шине {bus.ToString()} точку МКР{(i + 1).ToString()} номер", null, point.ToString(), goodText.Item2) { CanBeDeleted = true });

          if (!await GetIsIdleModeEnabled())
          {
            await Core.ModuleRelayControl.Functions.ConnectRelayAsync(mkr[i].IPAddress, bus, point);
            await Task.Delay(200);
          }
        }
      }
    }

    /// <summary>
    /// Подключает каждую точку к заданной шине, проверяет с помощью ППУ, и возвращает обратно.
    /// </summary>
    /// <param name="mkr">Список модулей МКР.</param>
    /// <param name="bus">Тип шины, к которой подключаются точки.</param>
    /// <param name="goodText">Текст для отображения в сообщениях.</param>
    public async Task ConnectAndTestPointsAsync(List<Core.ModuleRelayControl.Model> mkr, BusPoint bus)
    {
      await ShowMessageAsync(new ShowMessageModel($"Проверка точек", goodText.Item2) { CanBeDeleted = false });


      if (!int.TryParse(VoltageData.Text, out int voltage))
      {
        throw new ArgumentException("Некорректное значение напряжения.");
      }

      voltage = Math.Max(50, Math.Min(1000, voltage));
      if (voltage % 50 != 0)
      {
        voltage = ((voltage / 50) + 1) * 50;
      }

      VoltageData.Text = voltage.ToString();

      BusPoint negativeBus = BusPoint.A;
      if (bus == BusPoint.A)
      {
        negativeBus = BusPoint.B;
      }
      if (!await GetIsIdleModeEnabled())
      {
        await Core.GptLibrary.IrMode.SetModeAsync(gptLibrary);
        await Core.GptLibrary.IrMode.SetVoltageAsync(gptLibrary, voltage);
        await Core.GptLibrary.IrMode.SetTimeAsync(gptLibrary, time);
        await Core.DeviceBusCommutation.Functions.ConnectToBreakdownTester(deviceBusCommutation.IPAddress);
      }

      for (int i = 0; i < mkr.Count; i++)
      {
        var points = GetPoints(mkr[i], i + 1);
        foreach (var point in points)
        {
          ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();
          if (!await GetIsIdleModeEnabled())
          {
            await Task.Delay(10);

            await ShowMessageAsync(new ShowMessageModel($"\tОтключаем с шины {bus.ToString()} точку МКР{(i + 1).ToString()} номер", null, point.ToString(), goodText.Item2) { CanBeDeleted = true });
            await Core.ModuleRelayControl.Functions.DisconnectRelayAsync(mkr[i].IPAddress, bus, point);

            await ShowMessageAsync(new ShowMessageModel($"\tПодключаем к шине {negativeBus.ToString()} точку МКР{(i + 1).ToString()} номер", null, point.ToString(), goodText.Item2) { CanBeDeleted = true });
            await Core.ModuleRelayControl.Functions.ConnectRelayAsync(mkr[i].IPAddress, negativeBus, point);

            await TestPointWithPPUAsync(mkr[i], point);

            await ShowMessageAsync(new ShowMessageModel($"\tОтключаем с шины {negativeBus.ToString()} точку МКР{(i + 1).ToString()} номер", null, point.ToString(), goodText.Item2) { CanBeDeleted = true });
            await Core.ModuleRelayControl.Functions.DisconnectRelayAsync(mkr[i].IPAddress, negativeBus, point);

            await ShowMessageAsync(new ShowMessageModel($"\tПодключаем к шине {bus.ToString()} точку МКР{(i + 1).ToString()} номер", null, point.ToString(), goodText.Item2) { CanBeDeleted = true });
            await Core.ModuleRelayControl.Functions.ConnectRelayAsync(mkr[i].IPAddress, bus, point);
          }
        }
      }
    }

    /// <summary>
    /// Проверяет точку с помощью ППУ.
    /// </summary>
    /// <param name="mkr">Модуль МКР.</param>
    /// <param name="point">Точка для проверки.</param>
    private async Task TestPointWithPPUAsync(Core.ModuleRelayControl.Model mkr, int point)
    {
      await Task.Delay(100);
      double result = await Core.GptLibrary.IrMode.MeasureResistanceAsync(gptLibrary);
      bool error = result < electricalParameter ? true : false;
      await ShowMessageAsync(new ShowMessageModel($"\t\tРезультат проверки точки {point}", null, $"{result.ToString()} [{(error ? errorText.Item1.ToString() : goodText.Item1.ToString())}]", error ? errorText.Item2 : goodText.Item2) { CanBeDeleted = !error });
    }


    private List<int> GetPoints(Core.ModuleRelayControl.Model mkr, int moduleVoltageNumber)
    {
      int firstPoint = 1;
      int lastPoint = mkr.CountPoints;
      List<int> points = new List<int>();

      if (testDataModel.FirstPointModel.ModuleNumber == moduleVoltageNumber)
      {
        firstPoint = testDataModel.FirstPointModel.PointNumber;
      }

      if (testDataModel.LastPointModel.ModuleNumber == moduleVoltageNumber)
      {
        lastPoint = testDataModel.LastPointModel.PointNumber;
      }

      for (; firstPoint <= lastPoint; firstPoint++)
      {
        points.Add(firstPoint);
      }

      return points;
    }


    /// <summary>
    /// Асинхронно отображает сообщение.
    /// </summary>
    /// <param name="header">Текст заголовка сообщения.</param>
    /// <param name="headerColor">Цвет текста заголовка.</param>
    /// <param name="description">Текст описания сообщения.</param>
    /// <param name="descriptionColor">Цвет текста описания.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task<bool> ShowMessageAsync(ShowMessageModel showMessageModel) => await ProtocolSelfCheckControl.ShowMessageAsync(showMessageModel);
  }
}
