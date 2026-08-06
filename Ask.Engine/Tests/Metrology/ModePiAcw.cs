using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
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
  /// Реализует алгоритм выполнения метрологического контроля в режиме ПИ ACW.
  /// </summary>
  public class ModePiAcw : IExecution
  {
    /// <summary>
    /// Текущий метрологический режим - ПИ ACW.
    /// </summary>
    private MeasurementTypeCommand metrologicalModeRole => MeasurementTypeCommand.PI_ACW;

    /// <summary>
    /// Экземпляр объекта, инкапсулирующего логику проведения измерений и работу с оборудованием для данного режима.
    /// </summary>
    private PiMeasurement testMeasurement;

    /// <summary>
    /// Сервис взаимодействия с пользователем: вывод сообщений, запросы подтверждений, отображение результатов и ошибок.
    /// </summary>
    private IUserInteractionService _userInteractionService;

    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    public async void InitializeSettings(IExecutionController executionController, IUserInteractionService userInteractionService, IReferenceVoltageRequestService referenceVoltageRequestService)
    {
      _userInteractionService = userInteractionService;
      testMeasurement = new PiMeasurement(referenceVoltageRequestService);
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
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task ExecuteMeasurementProcess(ActionSettings settings, IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, _userInteractionService, metrologyMode: metrologicalModeRole, timeCheck: true, timeRampCheck: true);
      await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, metrologicalModeRole, _userInteractionService);
      await testMeasurement.SetupCommutation(_userInteractionService, data.FirstPoint, data.SecondPoint, metrologicalModeRole);
      await testMeasurement.ConfigureMeter(_userInteractionService, metrologicalModeRole, data);

      var (LowerBound, UpperBound, delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.PI_ACW, data.Param);
      await _userInteractionService.AppendEmptyLineAsync();
      await RangeMessages.PublishAllowedRangeAsync(
        VoltageUnit.Volt,
        new MeasurementRange(LowerBound, LowerBound, UpperBound),
        _userInteractionService);

      await UserActionHelper.RunWithUserRepeatAsync(async () => await testMeasurement.PerformMeasurement(metrologicalModeRole, data.Param, _userInteractionService), _userInteractionService, true);
    }

    public ITextAdapter GetControl()
    {
      return _userInteractionService;
    }

    private class PiMeasurement : BaseMeasurement
    {
      private IReferenceVoltageRequestService _reference;

      public PiMeasurement(IReferenceVoltageRequestService referenceVoltageRequestService) : base()
      {
        _reference = referenceVoltageRequestService;
      }

      /// <inheritdoc />
      public override async Task ConfigureMeter(IUserInteractionService messageService, MeasurementTypeCommand metrologicalModeRole, DataModel dataModel = null)
      {
        var breakDown = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IBreakdownTester>().FirstOrDefault() : null;
        string name = breakDown.Name;
        int chassis = breakDown.NumberChassis;
        int numer = breakDown.Number;

        await breakDown.ConnectableManager.InitializeAsync(messageService);
        await breakDown.AcwManger.Mode.SetModeAsync(messageService);
        await breakDown.AcwManger.Time.SetTestTimeAsync(dataModel.Time, messageService);
        await breakDown.AcwManger.Time.SetRampTimeAsync(dataModel.RampTime, messageService);
        await breakDown.AcwManger.FrequencyConfigurable.SetFrequencyAsync(50, messageService);
        await breakDown.AcwManger.CurrentLimits.SetLowCurrentLimitAsync(0, messageService);
        await breakDown.AcwManger.CurrentLimits.SetHighCurrentLimitAsync(40, messageService);
        await breakDown.AcwManger.Voltage.SetVoltageAsync(dataModel.Param, messageService);
      }

      /// <inheritdoc />
      public override async Task<bool> PerformMeasurement(MeasurementTypeCommand metrologicalModeRole, double param, IUserInteractionService userMessageService, double intrinsicValue = 0)
      {
        var meterDevice = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IBreakdownTester>().FirstOrDefault() : null;
        await MeasurementMessages.PublishTestVoltageOutputAsync(
          MeasurementTypeCommand.PI_ACW,
          param,
          userMessageService);

        (LowerBound, UpperBound, var delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.PI_ACW, param);

        MeasurementRange measurementRange = new MeasurementRange(param, LowerBound, UpperBound);
        await meterDevice.AcwManger.Measure.MeasureAsync(ElectricalTestFunction.DielectricWithstandAC, measurementRange);

        var result = await MeasuredReferenceMeter(userMessageService, param);

        var answer = result < LowerBound || result > UpperBound;
        var err = result - param;
        Measurements.Add(err);

        if (result < LowerBound || result > UpperBound)
        {
          AddMetrologyError(userMessageService, metrologicalModeRole, result, LowerBound, UpperBound, "В");
        }

        await MeasurementMessages.PublishResultAsync(MeasurementTypeCommand.KN_ACW, new MeasurementRange(result, LowerBound, UpperBound), result >= LowerBound && result <= UpperBound, outputService: userMessageService);
        await MeasurementMessages.PublishErrorAsync(
          VoltageUnit.Volt,
          new MeasurementRange(err, LowerBound, UpperBound),
          result >= LowerBound && result <= UpperBound,
          userMessageService);

        return true;
      }
      public override async Task FinalizeMeasurement(MeasurementTypeCommand metrologicalModeRole, IUserInteractionService messageService)
      {
        await PrintResult(messageService, MeasurementTypeCommand.PI_ACW);
        await RangeMessages.PublishAllowedRangeAsync(
          VoltageUnit.Volt,
          new MeasurementRange(LowerBound, LowerBound, UpperBound),
          messageService);
        await base.FinalizeMeasurement(metrologicalModeRole, messageService);

        Measurements.Clear();
      }

      private async Task<double> MeasuredReferenceMeter(IUserInteractionService userMessageService, double param)
      {
        var result = await _reference.RequestReferenceVoltageAsync(userMessageService.GetControl());
        return result == null ? -1 : result.Value;
      }
    }
  }
}
