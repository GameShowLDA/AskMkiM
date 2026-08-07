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
  /// Реализует алгоритм выполнения метрологического контроля в режиме ПИ DCW.
  /// </summary>
  public class ModePiDcw : IExecution
  {
    /// <summary>
    /// Текущий метрологический режим - ПИ DCW.
    /// </summary>
    private MeasurementTypeCommand metrologicalModeRole => MeasurementTypeCommand.PI_DCW;

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
    public void InitializeSettings(IExecutionController executionController, IUserInteractionService userInteractionService, IReferenceVoltageRequestService referenceVoltageRequestService)
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
    private async Task ExecuteMeasurementProcess(IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, _messageService, metrologyMode: metrologicalModeRole, timeCheck: true, timeRampCheck: true);
      await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, metrologicalModeRole, _messageService);
      await testMeasurement.SetupCommutation(_messageService, data.FirstPoint, data.SecondPoint, metrologicalModeRole);
      await testMeasurement.ConfigureMeter(_messageService, metrologicalModeRole, data);

      var (LowerBound, UpperBound, delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.PI_DCW, data.Param);

      await _messageService.AppendEmptyLineAsync();
      await UserActionHelper.RunWithUserRepeatAsync(async () => await testMeasurement.PerformMeasurement(metrologicalModeRole, data.Param, _messageService), _messageService, true);
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
        await breakDown.DcwManger.Mode.SetModeAsync(messageService);
        await breakDown.DcwManger.Time.SetTestTimeAsync(dataModel.Time, messageService);
        await breakDown.DcwManger.Time.SetRampTimeAsync(dataModel.RampTime, messageService);
        await breakDown.DcwManger.CurrentLimits.SetLowCurrentLimitAsync(0, messageService);
        await breakDown.DcwManger.CurrentLimits.SetHighCurrentLimitAsync(10, messageService);
        await breakDown.DcwManger.Voltage.SetVoltageAsync(dataModel.Param, messageService);
      }

      /// <inheritdoc />
      public override async Task<bool> PerformMeasurement(MeasurementTypeCommand metrologicalModeRole, double param, IUserInteractionService messageService, double intrinsicValue = 0)
      {
        var meterDevice = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IBreakdownTester>().FirstOrDefault() : null;
        await MeasurementMessages.PublishTestVoltageOutputAsync(CheckType.Metrology,
          MeasurementTypeCommand.PI_DCW,
          param,
          messageService);

        (LowerBound, UpperBound, var delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.PI_DCW, param);

        var result = await MeasuredReferenceMeter(messageService, param);

        MeasurementRange measurementRangeDcw = new MeasurementRange(param, LowerBound, UpperBound);
        await meterDevice.DcwManger.Measure.MeasureAsync(ElectricalTestFunction.DielectricWithstandDC, measurementRangeDcw);

        var answer = result < LowerBound || result > UpperBound;
        var err = result - param;

        Measurements.Add(err);

        if (result < LowerBound || result > UpperBound)
        {
          AddMetrologyError(messageService, metrologicalModeRole, result, LowerBound, UpperBound, "В");
        }

        await MeasurementMessages.PublishResultAsync(CheckType.Metrology, MeasurementTypeCommand.KN_DCW, new MeasurementRange(result, LowerBound, UpperBound), result >= LowerBound && result <= UpperBound, chains: MeasurementPointsDisplay, outputService: messageService);
        await PublishMetrologyMeasurementErrorAsync(
          VoltageUnit.Volt,
          new MeasurementRange(err, LowerBound, UpperBound),
          result >= LowerBound && result <= UpperBound,
          messageService);

        return true;
      }

      public override async Task FinalizeMeasurement(MeasurementTypeCommand metrologicalModeRole, IUserInteractionService messageService)
      {
        await PrintResult(messageService, MeasurementTypeCommand.PI_DCW);
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
