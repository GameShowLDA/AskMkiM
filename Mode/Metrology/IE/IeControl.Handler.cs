using Core.ConfigCollector;
using Mode.Base.SearchDevices;
using Utilities.Models;
using static AppConfig.Config.ExecutionConfig;
using static AppConfig.Config.MeasurementErrorConfig;
using static AppConfig.Data.MeasurementError.MeasurementErrorModel;

namespace Mode.Metrology.IE
{
  public partial class IeControl
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
    /// <param name="token">Токен отмены, позволяющий прервать операцию конфигурации.</param>
    public async Task ExecuteMeasurementProcess(CancellationToken token)
    {
      Completed = true;

      if (!await ValidateAndConnectDevice(token))
      {
        await ProtocolSelfCheckControl.AbortExecution();
      }

      await ConfigureDevices(token);
      await PerformCapacityMeasurement(token);

      ProtocolSelfCheckControl.ShowAdditionalFunctionButtons();
      Completed = false;
    }

    /// <summary>
    /// Выполняет измерение сопротивления.
    /// </summary>
    private async Task PerformCapacityMeasurement(CancellationToken token)
    {
      double result = 0;

      double.TryParse(measurementDataModel.ElectricParameterData.Text, out double capacity);
      ShowMessageModel showMessageModel = new ShowMessageModel("Измерение ёмкости", goodText.Item2);
      await ShowMessageAsync(showMessageModel);

      if (!await GetIsIdleModeEnabled())
      {
        result = await Task.Run(() =>
        {
          return meter.MeasureCapacitance();
        });
      }
      else
      {
        result = capacity;
      }

      double firstNorm = capacity - ((capacity / 100.0 * GetPercentageError(TypeCommand.IE)) + GetNumericError(TypeCommand.IE));
      double lastNorm = capacity + (capacity / 100.0 * GetPercentageError(TypeCommand.IE)) + GetNumericError(TypeCommand.IE);
      showMessageModel = new ShowMessageModel
      {
        Header = $"\tРезультат ёмкости ({firstNorm:F2}-{lastNorm:F2})",
        Message = $"{result:F2}",
        MessageColor = (result >= firstNorm && result <= lastNorm) ? goodText.Item2 : errorText.Item2,
      };
      await ShowMessageAsync(showMessageModel);
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

        // TODO : Переопределить мультиметр
        // meter = new Core.KeysightLibrary.Model();

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
    /// <param name="showMessageModel">Модель сообщения.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task<bool> ShowMessageAsync(ShowMessageModel showMessageModel) => await ProtocolSelfCheckControl.ShowMessageAsync(showMessageModel);
  }
}
