using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Engine.Tests.MethodExecutor.MeasurementSystem;
using Ask.Engine.Tests.Protocol;
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
    private async Task ExecuteMeasurementProcess(IUserInteractionService messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, messageService, timeCheck: true, timeRampCheck: true, voltageCheck: true, busCheck: true);
      PiACWMethodExecutorMeasurement testMeasurement = new PiACWMethodExecutorMeasurement();
      try
      {
        var connect = await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, messageService);
        if (!connect.Connect)
        {
          await messageService.ShowMessageAsync(new ShowMessageModel("Ошибка", message: connect.Message, type: ShowMessageModel.MessageType.Error), SkipStepModeCheck: true);
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

        await messageService.ShowMessageAsync(new ShowMessageModel("\tИспытания прочности изоляции(ACW)"));
        await UserActionHelper.RunWithUserRepeatAsync(async () =>
        {
          messageService.GetCancellationToken().ThrowIfCancellationRequested();
          var answer = await breakDown.AcwManger.Measure.MeasureAsync(dataModel.Param);
          var type = ShowMessageModel.MessageType.Success;
          if (answer.value >= dataModel.Param)
          {
            type = ShowMessageModel.MessageType.Error;
          }

          var dischargeIndex = CurrentDischargeNumber - 1;
          var bitString = GetBitString();
          var formattedResult = GroupMethodProtocolBuilder.FormatValue(answer.value, CurrentUnit.MilliAmpere);
          var resultMessage = new ShowMessageModel(
            $"Результат измерения разряда {dischargeIndex} ({bitString})",
            message: formattedResult,
            type: type)
          {
            IndentLevel = 2,
            ExecutionErrorMessage = type == ShowMessageModel.MessageType.Error
              ? GroupMethodProtocolBuilder.BuildFailure(
                dischargeIndex,
                bitString,
                dataModel.Param,
                answer.value,
                CurrentUnit.MilliAmpere,
                MeasurementLimitKind.Maximum)
              : null,
          };
          await messageService.ShowMessageAsync(resultMessage, skipPause: true);

          return type == ShowMessageModel.MessageType.Success;

        }, messageService);
      }

      public override async Task FinalizeAsync(IUserInteractionService messageService)
      {
        await base.FinalizeAsync(messageService);
      }
    }
  }
}
