using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Engine.Tests.Protocol;
using static Ask.Engine.Tests.Base.UIValidationHelper;

namespace Ask.Engine.Tests.NodeMethod.PI
{
  public class PiACWNodeMethodExecutor
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
    private async Task ExecuteMeasurementProcess(IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, _messageService, metrologyMode: MeasurementTypeCommand.PI_ACW, timeCheck: true, timeRampCheck: true, voltageCheck: true, busCheck: true);
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

        await breakDown.ConnectableManager.InitializeAsync(messageService);
        await breakDown.AcwManger.Mode.SetModeAsync(messageService);
        await breakDown.AcwManger.Time.SetTestTimeAsync(dataModel.Time, messageService);
        await breakDown.AcwManger.Time.SetRampTimeAsync(dataModel.RampTime, messageService);
        await breakDown.AcwManger.CurrentLimits.SetHighCurrentLimitAsync(dataModel.Param, messageService);
        await breakDown.AcwManger.FrequencyConfigurable.SetFrequencyAsync(50, messageService);
        await breakDown.AcwManger.Voltage.SetVoltageAsync(dataModel.Voltage, messageService);
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
            await protocolUI.ShowMessageAsync(new ShowMessageModel("\tИспытания прочности изоляции(ACW)"));

            await UserActionHelper.RunWithUserRepeatAsync(async () =>
            {
              token.ThrowIfCancellationRequested();
              var answer = await breakDown.AcwManger.Measure.MeasureAsync(ElectricalTestFunction.DielectricWithstandAC, dataModel.Param / 2, 0, dataModel.Param);
              var type = ShowMessageModel.MessageType.Success;

              if (answer.value >= dataModel.Param)
              {
                type = ShowMessageModel.MessageType.Error;
              }

              var formattedResult = NodeMethodProtocolBuilder.FormatValue(answer.value, CurrentUnit.MilliAmpere);
              var resultMessage = new ShowMessageModel(
                $"Результат измерения точки {connectResult.PointModel}",
                message: formattedResult,
                type: type)
              {
                IndentLevel = 2,
                ExecutionErrorMessage = type == ShowMessageModel.MessageType.Error
                  ? NodeMethodProtocolBuilder.BuildFailure(
                    connectResult.PointModel,
                    dataModel.Param,
                    answer.value,
                    CurrentUnit.MilliAmpere,
                    MeasurementLimitKind.Maximum)
                  : null,
              };
              await protocolUI.ShowMessageAsync(resultMessage, skipPause: true);

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
        ResetPoints();
      }
    }
  }
}
