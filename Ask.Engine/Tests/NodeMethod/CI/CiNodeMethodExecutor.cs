using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using static Ask.Engine.Tests.Base.UIValidationHelper;

namespace Ask.Engine.Tests.NodeMethod.CI
{
  public class CiNodeMethodExecutor
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
    private async Task ExecuteMeasurementProcess(IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, _messageService, timeCheck: true, voltageCheck: true);
      await NewCore.Communication.DeviceCommandSender.ResetAllSystem();

      CiNodeMethod testMeasurement = new CiNodeMethod();
      var connect = await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, _messageService);
      if (!connect.Connect)
      {
        await _messageService.ShowMessageAsync(new ShowMessageModel("Ошибка", message: connect.Message, type: ShowMessageModel.MessageType.Error));
        return;
      }

      await testMeasurement.SetupCommutation(_messageService, data.FirstPoint, data.SecondPoint, BusPoint.A);
      await testMeasurement.ConfigureMeter(_messageService, data);
      await testMeasurement.PerformMeasurement(_messageService, data);
      await testMeasurement.FinalizeAsync(_messageService);
    }

    private class CiNodeMethod : BaseNodeTest
    {
      public CiNodeMethod() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(IUserInteractionService messageService, DataModel dataModel = null)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
        string name = breakDown.Name;
        int chassis = breakDown.NumberChassis;
        int numer = breakDown.Number;

        await breakDown.ConnectableManager.InitializeAsync(messageService);
        await breakDown.IrManger.Mode.SetModeAsync(messageService);
        await breakDown.IrManger.Voltage.SetVoltageAsync(dataModel.Voltage, messageService);
        await breakDown.IrManger.Time.SetTestTimeAsync(dataModel.Time, messageService);
      }

      /// <inheritdoc />
      public override async Task PerformMeasurement(IUserInteractionService protocolUI, DataModel dataModel)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
        var token = protocolUI.GetCancellationToken();

        while (true)
        {
          token.ThrowIfCancellationRequested();

          var connectResult = await GetNextPoint(protocolUI);
          if (connectResult.Step)
          {
            await protocolUI.ShowMessageAsync(new ShowMessageModel("Измерение сопротивления изоляции"));

            await UserActionHelper.RunWithUserRepeatAsync(async () =>
            {
              token.ThrowIfCancellationRequested();
              var answer = await breakDown.IrManger.Measure.MeasureAsync(dataModel.Param, 1000, 60000, userMessageService: protocolUI);
              var type = ShowMessageModel.MessageType.Success;

              if (answer.value < dataModel.Param)
              {
                type = ShowMessageModel.MessageType.Error;
              }

              return type == ShowMessageModel.MessageType.Success;

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
      }
    }
  }
}
