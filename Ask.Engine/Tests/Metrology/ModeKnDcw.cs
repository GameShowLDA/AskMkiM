using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
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

      executionController.SetSettings(
        StartDelegate: ExecuteMeasurementProcess,
        true,
        StopDelegate: async (CancellationToken token) =>
        {
          await testMeasurement.FinalizeMeasurement(metrologicalModeRole, userInteractionService);
        });
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns></returns>
    private async Task ExecuteMeasurementProcess(IUserInteractionService userInteractionService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, userInteractionService);

      await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, metrologicalModeRole, userInteractionService);
      await testMeasurement.SetupCommutation(userInteractionService, data.FirstPoint, data.SecondPoint, metrologicalModeRole);
      await testMeasurement.ConfigureMeter(userInteractionService, metrologicalModeRole);
      await UserActionHelper.RunWithUserRepeatAsync(async () => await testMeasurement.PerformMeasurement(metrologicalModeRole, data.Param, userInteractionService), userInteractionService, true);
    }

    public ITextAdapter GetControl()
    {
      return _userInteractionService;
    }

    private class KnMeasurement : BaseMeasurement
    {
      private IReferenceVoltageRequestService _reference;
      public KnMeasurement(IReferenceVoltageRequestService referenceVoltageRequestService) : base()
      {
        _reference = referenceVoltageRequestService;
      }

      /// <inheritdoc />
      public override async Task ConfigureMeter(IUserInteractionService messageService, MeasurementTypeCommand metrologicalModeRole, DataModel dataModel = null)
      {
        await base.ConfigureMeter(messageService, metrologicalModeRole, dataModel);
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;

        await fastMeter.DcVoltageManager.SetDCVoltageModeAsync(messageService);
      }

      /// <inheritdoc />
      public override async Task<bool> PerformMeasurement(MeasurementTypeCommand metrologicalModeRole, double param, IUserInteractionService protocolUI, double intrinsicValue = 0)
      {
        protocolUI.GetCancellationToken().ThrowIfCancellationRequested();
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;

        var resultReferenceMeterMeasured = await MeasuredReferenceMeter(fastMeter, protocolUI, param);
        (LowerBound, UpperBound, var delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.KN_DCW, resultReferenceMeterMeasured);
        var resultFastMeterMeasured = await MeasuredFastMeter(fastMeter, protocolUI, param, LowerBound, UpperBound);

        await protocolUI.ShowMessageAsync(new ShowMessageModel(header: "Результат проверки"));
        var result = resultFastMeterMeasured >= LowerBound && resultFastMeterMeasured <= UpperBound;

        var err = resultFastMeterMeasured - resultReferenceMeterMeasured;
        Measurements.Add(err);

        await protocolUI.ShowMessageAsync(new ShowMessageModel($"Значение эталоного напряжения ", null, MeasurementValueFormatter.FormatWithUnit(resultReferenceMeterMeasured, "В")) { IndentLevel = 1 });
        await protocolUI.ShowMessageAsync(new ShowMessageModel("Результат измерения напряжение", message: MeasurementValueFormatter.FormatWithUnit(resultFastMeterMeasured, "В"), type: result ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 1 }, skipPause: true);
        await protocolUI.ShowMessageAsync(new ShowMessageModel("Диапазон допускаемых значений", message: $"от {MeasurementValueFormatter.Format(LowerBound)} до {MeasurementValueFormatter.Format(UpperBound)} В") { IndentLevel = 2 }, skipPause: true);
        await protocolUI.ShowMessageAsync(new ShowMessageModel("Погрешность измерения", message: MeasurementValueFormatter.FormatWithUnit(err, "В"), type: result ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 2 }, skipPause: true);

        return true;
      }

      public override async Task FinalizeMeasurement(MeasurementTypeCommand metrologicalModeRole, IUserInteractionService messageService)
      {
        await PrintResult(messageService, MeasurementTypeCommand.KN_DCW);
        await base.FinalizeMeasurement(metrologicalModeRole, messageService);
        Measurements.Clear();
      }

      private async Task<double> MeasuredFastMeter(IFastMeter fastMeter, IUserInteractionService userMessageService, double param, double rangeFrom, double rangeTo)
      {
        var result = await fastMeter.DcVoltageManager.MeasureDCVoltageAsync(param);
        return ApplyPpuDividerCoefficient(result, fastMeter.DcwPpuDividerCoefficientPercent);
      }

      private async Task<double> MeasuredReferenceMeter(IFastMeter fastMeter, IUserInteractionService userMessageService, double param)
      {
        var result = await _reference.RequestReferenceVoltageAsync(userMessageService.GetControl());
        return result == null ? -1 : result.Value;
      }
    }
  }
}
