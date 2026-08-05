using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.Tests.Metrology.MeasurementSystem;
using static Ask.Engine.Tests.Base.UIValidationHelper;

namespace Ask.Engine.Tests.Metrology
{
  /// <summary>
  /// Реализует алгоритм выполнения метрологического контроля в режиме КН DCW.
  /// </summary>
  public class ModeKnDcw
  {
    /// <summary>
    /// Текущий метрологический режим - КН DCW.
    /// </summary>
    private MeasurementTypeCommand metrologicalModeRole => MeasurementTypeCommand.KN_DCW;

    /// <summary>
    /// Экземпляр объекта, инкапсулирующего логику проведения измерений и работу с оборудованием для данного режима.
    /// </summary>
    private KnMeasurement testMeasurement;

    /// <summary>
    /// Сервис взаимодействия с пользователем: вывод сообщений, запросы подтверждений, отображение результатов и ошибок.
    /// </summary>
    private IUserInteractionService _userInteractionService;

    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    public void InitializeSettings(IExecutionController executionController, IUserInteractionService userInteractionService, IReferenceVoltageRequestService referenceVoltageRequestService)
    {
      _userInteractionService = userInteractionService;
      testMeasurement = new KnMeasurement(referenceVoltageRequestService);
      testMeasurement.SetExecutionController(executionController);

      ActionSettings settings = new ActionSettings()
      {
        StartDelegate = ExecuteMeasurementProcess,
        CheckType = CheckType.Metrology,
        StopDelegate = async (CancellationToken token) =>
        {
          await testMeasurement.FinalizeMeasurement(metrologicalModeRole, _userInteractionService);
        }
      };

      executionController.SetSettings(settings);
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns></returns>
    private async Task ExecuteMeasurementProcess(IUserInteractionService userInteractionService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, userInteractionService, metrologyMode: metrologicalModeRole);

      await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, metrologicalModeRole, userInteractionService);
      await testMeasurement.SetupCommutation(userInteractionService, data.FirstPoint, data.SecondPoint, metrologicalModeRole);
      await testMeasurement.ConfigureMeter(userInteractionService, metrologicalModeRole);
      await UserActionHelper.RunWithUserRepeatAsync(async () => await testMeasurement.PerformMeasurement(metrologicalModeRole, data.Param, userInteractionService), userInteractionService, true);
    }

    /// <summary>
    /// Возвращает адаптер текстового интерфейса пользователя.
    /// </summary>
    /// <returns>Экземпляр <see cref="ITextAdapter"/>.</returns>
    public ITextAdapter GetControl()
    {
      return _userInteractionService;
    }

    /// <summary>
    /// Реализует измерение постоянного напряжения в режиме КН.
    /// </summary>
    private class KnMeasurement : BaseMeasurement
    {
      /// <summary>
      /// Сервис получения значения эталонного напряжения.
      /// </summary>
      private IReferenceVoltageRequestService _reference;

      /// <summary>
      /// Инициализирует обработчик измерений режима КН.
      /// </summary>
      /// <param name="referenceVoltageRequestService">
      /// Сервис получения значения эталонного напряжения.
      /// </param>
      public KnMeasurement(IReferenceVoltageRequestService referenceVoltageRequestService) : base()
      {
        _reference = referenceVoltageRequestService;
      }

      /// <inheritdoc />
      public override async Task ConfigureMeter(IUserInteractionService messageService, MeasurementTypeCommand metrologicalModeRole, DataModel dataModel = null)
      {
        await base.ConfigureMeter(messageService, metrologicalModeRole, dataModel);
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IMultimeter>().FirstOrDefault() : null;

        await fastMeter.DcVoltageManager.SetDCVoltageModeAsync(messageService);
      }

      /// <inheritdoc />
      public override async Task<bool> PerformMeasurement(MeasurementTypeCommand metrologicalModeRole, double param, IUserInteractionService protocolUI, double intrinsicValue = 0)
      {
        protocolUI.GetCancellationToken().ThrowIfCancellationRequested();
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IMultimeter>().FirstOrDefault() : null;

        var resultReferenceMeterMeasured = await MeasuredReferenceMeter(fastMeter, protocolUI, param);
        (LowerBound, UpperBound, var delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.KN_DCW, resultReferenceMeterMeasured);

        MeasurementRange measurementRange = new MeasurementRange(param, LowerBound, UpperBound);
        var resultFastMeterMeasured = await MeasuredFastMeter(fastMeter, protocolUI, measurementRange);

        await protocolUI.ShowMessageAsync(new ShowMessageModel(header: "Результат проверки"));
        var result = resultFastMeterMeasured >= LowerBound && resultFastMeterMeasured <= UpperBound;

        var err = resultFastMeterMeasured - resultReferenceMeterMeasured;
        Measurements.Add(err);

        if (!result)
        {
          AddMetrologyError(protocolUI, metrologicalModeRole, resultFastMeterMeasured, LowerBound, UpperBound, "В");
        }

        await protocolUI.ShowMessageAsync(new ShowMessageModel($"Значение эталоного напряжения ", null, MeasurementValueFormatter.FormatWithUnit(resultReferenceMeterMeasured, "В")) { IndentLevel = 1 });
        await MeasurementMessages.PublishResultAsync(MeasurementTypeCommand.KN_DCW, new MeasurementRange(resultFastMeterMeasured, LowerBound, UpperBound), result, outputService: protocolUI);
        await RangeMessages.PublishAllowedRangeAsync(
          VoltageUnit.Volt,
          new MeasurementRange(LowerBound, LowerBound, UpperBound),
          protocolUI,
          indentLevel: 2);
        await MeasurementMessages.PublishErrorAsync(
          VoltageUnit.Volt,
          new MeasurementRange(err, LowerBound, UpperBound),
          result,
          protocolUI);

        return true;
      }

      /// <inheritdoc />
      public override async Task FinalizeMeasurement(MeasurementTypeCommand metrologicalModeRole, IUserInteractionService messageService)
      {
        await PrintResult(messageService, MeasurementTypeCommand.KN_DCW);
        await base.FinalizeMeasurement(metrologicalModeRole, messageService);
        Measurements.Clear();
      }

      /// <summary>
      /// Выполняет измерение напряжения проверяемым мультиметром.
      /// </summary>
      /// <param name="fastMeter">Проверяемый мультиметр.</param>
      /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
      /// <param name="param">Ожидаемое значение напряжения.</param>
      /// <param name="rangeFrom">Нижняя граница допустимого диапазона.</param>
      /// <param name="rangeTo">Верхняя граница допустимого диапазона.</param>
      /// <returns>Измеренное значение напряжения.</returns>
      private async Task<double> MeasuredFastMeter(IMultimeter fastMeter, IUserInteractionService userMessageService, MeasurementRange measurementRange)
      {
        var result = await fastMeter.DcVoltageManager.MeasureDCVoltageAsync(measurementRange, userMessageService);
        return result;
      }

      /// <summary>
      /// Получает значение напряжения с эталонного средства измерения.
      /// </summary>
      /// <param name="fastMeter">
      /// Проверяемый мультиметр. Параметр зарезервирован для совместимости.
      /// </param>
      /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
      /// <param name="param">
      /// Номинальное значение напряжения. Параметр зарезервирован для совместимости.
      /// </param>
      /// <returns>
      /// Значение эталонного напряжения либо <c>-1</c>, если получить его не удалось.
      /// </returns>
      private async Task<double> MeasuredReferenceMeter(IMultimeter fastMeter, IUserInteractionService userMessageService, double param)
      {
        var result = await _reference.RequestReferenceVoltageAsync(userMessageService.GetControl());
        return result == null ? -1 : result.Value;
      }
    }
  }
}
