using Ask.Core.Services.Errors.Device.Adapters;
using Ask.Core.Services.Errors.Device.Breakdown;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using static Ask.Engine.Tests.Base.UIValidationHelper;

namespace Ask.Engine.Tests.NodeMethod.PI
{
  public class PiDCWNodeMethodExecutor
  {
    private PiNodeMethod testMeasurement = new PiNodeMethod();

    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    public void InitializeSettings(IExecutionController executionController, IUserInteractionService userInteractionService)
    {
      executionController.SetSettings(StartDelegate: ExecuteMeasurementProcess, true, StopDelegate: async (CancellationToken token) =>
      {
        await testMeasurement.FinalizeAsync(userInteractionService);
      });
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task ExecuteMeasurementProcess(IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, _messageService, timeCheck: true, timeRampCheck: true, voltageCheck: true, busCheck: true);
      await NewCore.Communication.DeviceCommandSender.ResetAllSystem();

      var connect = await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, _messageService);
      if (!connect.Connect)
      {
        await _messageService.ShowMessageAsync(new ShowMessageModel("Ошибка", message: connect.Message, type: ShowMessageModel.MessageType.Error));
        return;
      }

      await testMeasurement.SetupCommutation(_messageService, data.FirstPoint, data.SecondPoint, data.ActiveBus);
      await testMeasurement.ConfigureMeter(_messageService, data);
      await testMeasurement.PerformMeasurement(_messageService, data);
    }

    private class PiNodeMethod : BaseNodeTest
    {
      public PiNodeMethod() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(IUserInteractionService messageService, DataModel dataModel = null)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
        string name = breakDown.Name;
        int chassis = breakDown.NumberChassis;
        int numer = breakDown.Number;

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.ConnectableManager.InitializeAsync(messageService)).Connect, messageService))
          throw ConnectionExceptionAdapter.ConnectFailed(name, chassis, numer);

        await breakDown.DcwManger.Mode.SetModeAsync(messageService);
        await breakDown.DcwManger.Voltage.SetVoltageAsync(dataModel.Voltage, messageService);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.Time.SetTestTimeAsync(dataModel.Time, messageService)).Success, messageService))
          throw DcwExceptionFactory.SetTestTimeFailed(name, chassis, numer);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.Time.SetRampTimeAsync(dataModel.RampTime, messageService)).Success, messageService))
          throw DcwExceptionFactory.SetRampTimeFailed(name, chassis, numer);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.CurrentLimits.SetHighCurrentLimitAsync(dataModel.Param, messageService)).Success, messageService))
          throw DcwExceptionFactory.SetHighLimitFailed(name, chassis, numer);

      }

      /// <inheritdoc />
      public override async Task PerformMeasurement(IUserInteractionService protocolUI, DataModel dataModel)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
        var token = protocolUI.GetCancellationToken();

        while (true)
        {
          token.ThrowIfCancellationRequested();

          protocolUI.GetCancellationToken();

          var connectResult = await GetNextPoint(protocolUI);
          if (connectResult.Step)
          {
            await protocolUI.ShowMessageAsync(new ShowMessageModel("\tИспытания прочности изоляции(DCW)"));

            await UserActionHelper.RunWithUserRepeatAsync(async () =>
            {
              token.ThrowIfCancellationRequested();

              var answer = await breakDown.DcwManger.Measure.MeasureAsync();
              var type = ShowMessageModel.MessageType.Success;

              if (answer.value >= dataModel.Param)
              {
                type = ShowMessageModel.MessageType.Error;
              }

              return type == ShowMessageModel.MessageType.Success ? true : false;
            }, protocolUI);
          }
          else
          {
            break;
          }
        }
      }

      public override async Task FinalizeAsync(IUserInteractionService messageService)
      {
        await base.FinalizeAsync(messageService);
        ResetPoints();
      }
    }
  }
}
