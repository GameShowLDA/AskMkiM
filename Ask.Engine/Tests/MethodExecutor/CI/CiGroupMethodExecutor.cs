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

namespace Ask.Engine.Tests.MethodExecutor.CI
{
  public class CiGroupMethodExecutor
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
    private async Task ExecuteMeasurementProcess(IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, _messageService, metrologyMode: MeasurementTypeCommand.SI, timeCheck: true, voltageCheck: true, busCheck: true);
      TestMeasurement testMeasurement = new TestMeasurement();
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

    private class TestMeasurement : BaseMethodExecutor
    {
      public TestMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(IUserInteractionService messageService, DataModel dataModel = null)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
        var name = breakDown.Name;
        var chassis = breakDown.NumberChassis;
        var number = breakDown.Number;

        await breakDown.ConnectableManager.InitializeAsync(messageService);
        await breakDown.IrManger.Mode.SetModeAsync(messageService);
        await breakDown.IrManger.Voltage.SetVoltageAsync(dataModel.Voltage, messageService);
        await breakDown.IrManger.Time.SetTestTimeAsync(dataModel.Time, messageService);
      }

      /// <inheritdoc />
      public override async Task PerformMeasurement(IUserInteractionService messageService, DataModel dataModel)
      {
        await UserActionHelper.RunWithUserRepeatAsync(async () =>
        {
          messageService.GetCancellationToken().ThrowIfCancellationRequested();

          var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
          await MeasurementMessages.PublishStartAsync(
            MeasurementTypeCommand.SI,
            messageService,
            indentLevel: 1);

          MeasurementRange measurementRange = new MeasurementRange(dataModel.Param, dataModel.Param, 60000);
          var answer = await breakDown.IrManger.Measure.MeasureAsync(ElectricalTestFunction.InsulationResistance, measurementRange, userMessageService: messageService);

          bool isSuccessful = answer.value >= dataModel.Param;

          var dischargeIndex = CurrentDischargeNumber - 1;
          var bitString = GetBitString();
          string? executionErrorMessage = !isSuccessful
            ? MeasurementMessages.BuildGroupFailure(
              dischargeIndex,
              bitString,
              dataModel.Param,
              answer.value,
              ResistanceUnit.MegaOhm,
              MeasurementLimitKind.Minimum)
            : null;
          await MeasurementMessages.PublishResultAsync(
            ResistanceUnit.MegaOhm,
            new MeasurementRange(answer.value, dataModel.Param, -1),
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


