using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Engine.Tests.MethodExecutor.MeasurementSystem;
using static Ask.Engine.Tests.Base.UIValidationHelper;

namespace Ask.Engine.Tests.MethodExecutor.PI
{
  public class PiDcwGroupMethodExecutor
  {
    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    public void InitializeSettings(IExecutionController executionController)
    {
      ActionSettings settings = new ActionSettings()
      {
        StartDelegate = ExecuteMeasurementProcess,
        CheckType = CheckType.Test,
        AccumulateErrorMessages = true,
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
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, _messageService, metrologyMode: MeasurementTypeCommand.PI_DCW, timeCheck: true, timeRampCheck: true, voltageCheck: true, busCheck: true);

      PiDCWMethodExecutorMeasurement testMeasurement = new PiDCWMethodExecutorMeasurement();
      try
      {
        var connect = await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, _messageService);
        if (!connect.Connect)
        {
          await ExecutionMessages.PublishErrorAsync(
            connect.Message,
            _messageService,
            skipStepModeCheck: true);
          return;
        }

        await testMeasurement.SetupCommutation(_messageService, data.FirstPoint, data.SecondPoint, data.ActiveBus);
        await testMeasurement.ConfigureMeter(_messageService, data);
        await testMeasurement.RunParallelModuleTasksAsync(_messageService, data);
      }
      finally
      {
        await testMeasurement.FinalizeAsync(_messageService);
      }
    }

    private class PiDCWMethodExecutorMeasurement : BaseMethodExecutor
    {
      public PiDCWMethodExecutorMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(IUserInteractionService messageService, DataModel dataModel = null)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
        var name = breakDown.Name;
        var chassis = breakDown.NumberChassis;
        var number = breakDown.Number;

        await breakDown.ConnectableManager.InitializeAsync(messageService);
        await breakDown.DcwManger.Mode.SetModeAsync(messageService);
        await breakDown.DcwManger.Voltage.SetVoltageAsync(dataModel.Voltage, messageService);
        await breakDown.DcwManger.Time.SetTestTimeAsync(dataModel.Time, messageService);
        await breakDown.DcwManger.Time.SetRampTimeAsync(dataModel.RampTime, messageService);
        await breakDown.DcwManger.CurrentLimits.SetHighCurrentLimitAsync(dataModel.Param, messageService);
      }

      /// <inheritdoc />
      public override async Task PerformMeasurement(IUserInteractionService messageService, DataModel dataModel)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();

        await MeasurementMessages.PublishLeakageCurrentStartAsync(CheckType.Test,
          MeasurementTypeCommand.PI_DCW,
          messageService);
        await UserActionHelper.RunWithUserRepeatAsync(async () =>
        {
          MeasurementRange measurementRange = new MeasurementRange(dataModel.Param / 2, 0, dataModel.Param);
          var answer = await breakDown.DcwManger.Measure.MeasureAsync(ElectricalTestFunction.DielectricWithstandDC, measurementRange, userMessageService: messageService);

          bool isSuccessful = answer.value < dataModel.Param;

          var dischargeIndex = CurrentDischargeNumber - 1;
          var bitString = GetBitString();
          string? executionErrorMessage = !isSuccessful
            ? MeasurementMessages.BuildGroupFailure(
              dischargeIndex,
              bitString,
              dataModel.Param,
              answer.value,
              CurrentUnit.MilliAmpere,
              MeasurementLimitKind.Maximum)
            : null;
          await MeasurementMessages.PublishResultAsync(CheckType.Test,
            CurrentUnit.MilliAmpere,
            new MeasurementRange(answer.value, 0, dataModel.Param),
            isSuccessful,
            $"Разряд {dischargeIndex} ({bitString})",
            executionErrorMessage,
            messageService);

          return isSuccessful;

        }, messageService);
      }

      public override async Task FinalizeAsync(IUserInteractionService messageService)
      {
        await base.FinalizeAsync(messageService);
      }
    }
  }
}
