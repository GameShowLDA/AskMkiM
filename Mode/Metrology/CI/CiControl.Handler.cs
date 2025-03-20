using Core.Communication;
using Core.ConfigCollector;
using Mode.Base.SearchDevices;
using Utilities.Models;
using static AppConfig.Config.ExecutionConfig;

namespace Mode.Metrology.CI
{
  /// <summary>
  /// Частичная реализация класса управления процессом измерения сопротивления изоляции.
  /// </summary>
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
    /// Последовательно выполняет валидацию данных, подключение к устройствам, конфигурацию устройств,
    /// выполнение измерения и обновление пользовательского интерфейса после завершения измерения.
    /// </summary>
    /// <param name="token">Токен отмены для прерывания процесса измерения.</param>
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
    /// Производит проверку и коррекцию введённого напряжения, отображает сообщение о начале измерения,
    /// задаёт напряжение на устройстве, получает результат измерения и отображает его.
    /// После измерения отключает устройство GPT и выполняет сброс системы.
    /// </summary>
    /// <param name="token">Токен отмены для прерывания операции измерения.</param>
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

      double result = !await GetIsIdleModeEnabled()
                      ? await Core.GptLibrary.IrMode.MeasureResistanceAsync(gptLibrary as Core.GptLibrary.Model)
                      : resistance;
      await ShowMessageAsync(new ShowMessageModel($"\tРезультат измерения при {voltage}В", null, $"{result} МОм"));
      await Task.Delay(1000);

      gptLibrary.Disconnect();
      await CommunicationManager.ResetAllSystem();
    }

    /// <summary>
    /// Проверяет корректность введённых данных и подключается к устройствам.
    /// Выполняется валидация первой и второй точек измерения, проверка их уникальности,
    /// а также установка необходимых моделей для дальнейшей работы.
    /// Если устройства не могут быть подключены, операция прерывается.
    /// </summary>
    /// <param name="token">Токен отмены для прерывания операции.</param>
    /// <returns>
    /// <c>true</c>, если проверка и подключение устройств прошли успешно; в противном случае — <c>false</c>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если происходит ошибка в процессе подключения.</exception>
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
    /// Асинхронно отображает сообщение, используя делегат для вывода сообщений.
    /// Это прокси‑метод, который вызывает соответствующую функцию в ProtocolSelfCheckControl.
    /// </summary>
    /// <param name="showMessageModel">Модель сообщения, содержащая заголовок, описание и цветовую схему.</param>
    /// <returns>Задача, представляющая асинхронную операцию отображения сообщения.</returns>
    public async Task<bool> ShowMessageAsync(ShowMessageModel showMessageModel) =>
      await ProtocolSelfCheckControl.ShowMessageAsync(showMessageModel);
  }
}
