using Core.ConfigCollector;
using Mode.Base.SearchDevices;
using Utilities.Models;
using static AppConfig.Config.ExecutionConfig;
using static AppConfig.Config.MeasurementErrorConfig;
using static AppConfig.Data.MeasurementError.MeasurementErrorModel;

namespace Mode.Metrology.KC
{
  public partial class KcControl
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
    /// Главный метод, который управляет процессом измерения сопротивления.
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
      await PerformResistanceMeasurement(token);

      ProtocolSelfCheckControl.ShowAdditionalFunctionButtons();
      Completed = false;
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
    /// Выполняет измерение сопротивления.
    /// </summary>
    private async Task PerformResistanceMeasurement(CancellationToken token)
    {
      double result = 0;

      double.TryParse(measurementDataModel.ElectricParameterData.Text, out double resistance);

      await ShowMessageAsync(new ShowMessageModel("Измерение сопротивления", goodText.Item2));

      if (!await GetIsIdleModeEnabled())
      {
        result = await Task.Run(() =>
        {
          return meter.MeasureResistance();
        });
      }
      else
      {
        result = resistance;
      }

      double firstNorm = resistance - ((resistance / 100.0 * GetPercentageError(TypeCommand.KC)) + GetNumericError(TypeCommand.KC));
      double lastNorm = resistance + (resistance / 100.0 * GetPercentageError(TypeCommand.KC)) + GetNumericError(TypeCommand.KC);

      ShowMessageModel showMessageModel = new ShowMessageModel($"\tРезультат сопротивления ({firstNorm:F2}-{lastNorm:F2})", null, $"{result:F2}");
      showMessageModel.MessageColor = (result >= firstNorm && result <= lastNorm) ? goodText.Item2 : errorText.Item2;
      showMessageModel.ExecutionError = (result >= firstNorm && result <= lastNorm) ? false : true;
      showMessageModel.CanBeDeleted = showMessageModel.ExecutionError;

      await ShowMessageAsync(showMessageModel);
    }

    /// <summary>
    /// Асинхронно отображает сообщение.
    /// </summary>
    /// <param name="showMessageModel">Модель сообщения.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task<bool> ShowMessageAsync(ShowMessageModel showMessageModel) => await ProtocolSelfCheckControl.ShowMessageAsync(showMessageModel);
  }
}
