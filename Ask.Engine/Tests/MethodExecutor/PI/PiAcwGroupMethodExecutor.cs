using Ask.Core.Services.Errors.Device.Adapters;
using Ask.Core.Services.Errors.Device.Breakdown;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
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
      executionController.SetSettings(StartDelegate: ExecuteMeasurementProcess, true, null);
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

        await breakDown.ConnectableManager.InitializeAsync(messageService);
        await breakDown.AcwManger.Voltage.SetVoltageAsync(dataModel.Param, messageService);
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
          var answer = await breakDown.AcwManger.Measure.MeasureAsync(dataModel.Param, userMessageService: messageService);
          var type = ShowMessageModel.MessageType.Success;
          if (answer.value > dataModel.Param)
          {
            type = ShowMessageModel.MessageType.Error;
          }

          return type == ShowMessageModel.MessageType.Success ? true : false;

        }, messageService);
      }

      public override async Task FinalizeAsync(IUserInteractionService messageService)
      {
        await base.FinalizeAsync(messageService);
      }
    }
  }
}
