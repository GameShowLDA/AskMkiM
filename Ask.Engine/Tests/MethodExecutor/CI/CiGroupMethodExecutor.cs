using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
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
      executionController.SetSettings(StartDelegate: ExecuteMeasurementProcess, true);
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task ExecuteMeasurementProcess(IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, _messageService, timeCheck: true, voltageCheck: true, busCheck: true);
      TestMeasurement testMeasurement = new TestMeasurement();
      try
      {
        var connect = await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, _messageService);
        if (!connect.Connect)
        {
          await _messageService.ShowMessageAsync(new ShowMessageModel("Ошибка", message: connect.Message, type: ShowMessageModel.MessageType.Error), SkipStepModeCheck: true);
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
          await messageService.ShowMessageAsync(new ShowMessageModel("\tИзмерение сопротивления изоляции"));

          var answer = await breakDown.IrManger.Measure.MeasureAsync(dataModel.Param, dataModel.Param, 60000, userMessageService: messageService);
          var type = ShowMessageModel.MessageType.Success;
          if (answer.value < dataModel.Param)
          {
            type = ShowMessageModel.MessageType.Error;
          }

          await messageService.ShowMessageAsync(new ShowMessageModel($"\t\tРезультат измерения разряда {HighestBitCount}({GetBitString()})", message: $"{answer.ToString()} МОм", type: type), skipPause: true);
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
