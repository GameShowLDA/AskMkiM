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
  public class PiAcwGroupMethodExecutor
  {
    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    public void InitializeSettings(IExecutionController executionController)
    {
      ActionSettings settings = new ActionSettings
      {
        StartDelegate = ExecuteMeasurementProcess,
        StopDelegate = null,
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
    private async Task ExecuteMeasurementProcess(ActionSettings settings, IUserInteractionService messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, messageService, metrologyMode: MeasurementTypeCommand.PI_ACW, timeCheck: true, timeRampCheck: true, voltageCheck: true, busCheck: true);
      PiACWMethodExecutorMeasurement testMeasurement = new PiACWMethodExecutorMeasurement();
      try
      {
        var connect = await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, messageService);
        if (!connect.Connect)
        {
          await ExecutionMessages.PublishErrorAsync(
            connect.Message,
            messageService,
            skipStepModeCheck: true);
          return;
        }

        await testMeasurement.SetupCommutation(messageService, data.FirstPoint, data.SecondPoint, data.ActiveBus);
        await testMeasurement.ConfigureMeter(messageService, data);
        await testMeasurement.RunParallelModuleTasksAsync(messageService, data);
      }
      finally
      {
        await testMeasurement.FinalizeAsync(messageService);
      }
    }

    private class PiACWMethodExecutorMeasurement : BaseMethodExecutor
    {
      public PiACWMethodExecutorMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(IUserInteractionService messageService, DataModel dataModel = null)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
        var name = breakDown.Name;
        var chassis = breakDown.NumberChassis;
        var number = breakDown.Number;

        await breakDown.AcwManger.Mode.SetModeAsync(messageService);
        await breakDown.AcwManger.Voltage.SetVoltageAsync(dataModel.Voltage, messageService);
        await breakDown.AcwManger.Time.SetTestTimeAsync(dataModel.Time, messageService);
        await breakDown.AcwManger.Time.SetRampTimeAsync(dataModel.RampTime, messageService);
        await breakDown.AcwManger.CurrentLimits.SetHighCurrentLimitAsync(dataModel.Param, messageService);
        await breakDown.AcwManger.FrequencyConfigurable.SetFrequencyAsync(50, messageService);
      }

      /// <inheritdoc />
      public override async Task PerformMeasurement(IUserInteractionService messageService, DataModel dataModel)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();

        await MeasurementMessages.PublishLeakageCurrentStartAsync(
          MeasurementTypeCommand.PI_ACW,
          messageService);
        await UserActionHelper.RunWithUserRepeatAsync(async () =>
        {
          messageService.GetCancellationToken().ThrowIfCancellationRequested();

          MeasurementRange measurementRange = new MeasurementRange(dataModel.Param / 2, 0, dataModel.Param);
          var answer = await breakDown.AcwManger.Measure.MeasureAsync(ElectricalTestFunction.DielectricWithstandAC, measurementRange, userMessageService: messageService);

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
          await MeasurementMessages.PublishResultAsync(
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
