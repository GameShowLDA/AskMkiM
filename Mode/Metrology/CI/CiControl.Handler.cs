using Core.Communication;
using Core.ConfigCollector;
using Mode.Base.SearchDevices;
using Utilities.Models;
using static AppConfig.Config.ExecutionConfig;

namespace Mode.Metrology.CI
{
  public partial class CiControl
  {
    /// <summary>
    /// Получает или устанавливает значение, указывающее, завершено ли измерение.
    /// При установке значения обновляет состояние доступности полей измерения.
    /// </summary>
    private bool Completed
    {
      get => completed;
      set
      {
        measurementDataModel.FirstPointData.IsReadOnly = value;
        measurementDataModel.LastPointData.IsReadOnly = value;
        measurementDataModel.ElectricParameterData.IsReadOnly = value;
        completed = value;
      }
    }

    /// <summary>
    /// Главный метод, который управляет процессом измерения сопротивления изоляции.
    /// </summary>
    public async Task ExecuteMeasurementProcess(CancellationToken token)
    {
      Completed = true;

      if (!await ValidateAndConnectDevice(token))
      {
        await ProtocolSelfCheckControl.AbortExecution();
      }

      await ConfigureDevices(token);
      await PerformInsulationResistanceMeasurement(token);

      ProtocolSelfCheckControl.ShowAdditionalFunctionButtons();
      Completed = false;
    }

    /// <summary>
    /// Выполняет измерение сопротивления изоляции.
    /// </summary>
    private async Task PerformInsulationResistanceMeasurement(CancellationToken token)
    {
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

      await ShowMessageAsync(new ShowMessageModel("Измерение сопротивления изоляции", goodText.Item2));
      double.TryParse(measurementDataModel.ElectricParameterData.Text, out double resistance);


      token.ThrowIfCancellationRequested();

      if (!await GetIsIdleModeEnabled())
      {
        await Core.GptLibrary.IrMode.SetVoltageAsync(gptLibrary as Core.GptLibrary.Model, voltage);
      }

      double result = !await GetIsIdleModeEnabled() ? await Core.GptLibrary.IrMode.MeasureResistanceAsync(gptLibrary as Core.GptLibrary.Model) : resistance;
      await ShowMessageAsync(new ShowMessageModel($"\tРезультат измерения при {voltage}В", null, $"{result} МОм"));
      await Task.Delay(1000);


      gptLibrary.Disconnect();
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

        var firstPoint = await inputValidator.ValidateMeasurementPointAsync(ShowMessageAsync, measurementDataModel.FirstPointData.Text);
        var secondPoint = await inputValidator.ValidateMeasurementPointAsync(ShowMessageAsync, measurementDataModel.LastPointData.Text);
        if (!firstPoint.IsSuccess || !secondPoint.IsSuccess)
        {
          return false;
        }
        if (!inputValidator.ValidateUniqueMeasurementPointAsync(firstPoint.PointModel, secondPoint.PointModel))
        {
          return false;
        }

        measurementDataModel.FirstPointModel = firstPoint.PointModel;
        measurementDataModel.LastPointModel = secondPoint.PointModel;

        measurementDataModel.ManagerShassy = firstPoint.ManagerShassy;
        measurementDataModel.FirstModuleRelayControl = firstPoint.ModuleRelayControl;
        measurementDataModel.LastModuleRelayControl = secondPoint.ModuleRelayControl;

        deviceBusCommutation = ConfigCollector.GetDeviceBusCommutation();
        gptLibrary = Core.GptLibrary.Model.CreateAsync();

        if (!await AttemptDeviceConnection())
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

      return true;
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
