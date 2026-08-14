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
      ActionSettings settings = new ActionSettings()
      {
        StartDelegate = ExecuteMeasurementProcess,
        CheckType = CheckType.Test,
        AccumulateErrorMessages = true,
        StopDelegate = async (CancellationToken token) =>
        {
          await testMeasurement.FinalizeAsync(userInteractionService);
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
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, _messageService, metrologyMode: MeasurementTypeCommand.PI_DCW, timeCheck: true, timeRampCheck: true, voltageCheck: true, busCheck: true);
      var connect = await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, _messageService);
      if (!connect.Connect)
      {
        await ExecutionMessages.PublishErrorAsync(connect.Message, _messageService);
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

        await breakDown.ConnectableManager.InitializeAsync(messageService);
        await breakDown.DcwManger.Mode.SetModeAsync(messageService);
        await breakDown.DcwManger.Voltage.SetVoltageAsync(dataModel.Voltage, messageService);
        await breakDown.DcwManger.Time.SetTestTimeAsync(dataModel.Time, messageService);
        await breakDown.DcwManger.Time.SetRampTimeAsync(dataModel.RampTime, messageService);
        await breakDown.DcwManger.CurrentLimits.SetHighCurrentLimitAsync(dataModel.Param, messageService);
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
            await MeasurementMessages.PublishLeakageCurrentStartAsync(CheckType.Test,
              MeasurementTypeCommand.PI_DCW,
              protocolUI);

            await UserActionHelper.RunWithUserRepeatAsync(async () =>
            {
              token.ThrowIfCancellationRequested();

              MeasurementRange measurementRange = new MeasurementRange(dataModel.Param / 2, 0, dataModel.Param);
              var answer = await breakDown.DcwManger.Measure.MeasureAsync(ElectricalTestFunction.DielectricWithstandDC, measurementRange);

              bool isSuccessful = answer.value < dataModel.Param;

              string? executionErrorMessage = !isSuccessful
                ? MeasurementMessages.BuildNodeFailure(
                  connectResult.PointModel,
                  dataModel.Param,
                  answer.value,
                  CurrentUnit.MilliAmpere,
                  MeasurementLimitKind.Maximum)
                : null;
              await MeasurementMessages.PublishResultAsync(CheckType.Test,
                CurrentUnit.MilliAmpere,
                new MeasurementRange(answer.value, 0, dataModel.Param),
                isSuccessful,
                connectResult.PointModel.ToString(),
                executionErrorMessage,
                protocolUI);

              return isSuccessful;
            }, protocolUI, measurementTask: true);
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
