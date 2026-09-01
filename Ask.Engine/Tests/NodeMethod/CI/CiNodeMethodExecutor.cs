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
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, _messageService, metrologyMode: MeasurementTypeCommand.SI, timeCheck: true, voltageCheck: true);
      CiNodeMethod testMeasurement = new CiNodeMethod();
      try
      {
        var connect = await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, _messageService);
        if (!connect.Connect)
        {
          await ExecutionMessages.PublishErrorAsync(connect.Message, _messageService);
          return;
        }

        await testMeasurement.SetupCommutation(_messageService, data.FirstPoint, data.SecondPoint, BusPoint.A);
        await testMeasurement.ConfigureMeter(_messageService, data);
        await testMeasurement.PerformMeasurement(_messageService, data);
      }
      finally
      {
        await testMeasurement.FinalizeAsync(_messageService);
      }
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
            await MeasurementMessages.PublishStartAsync(CheckType.Test,
              MeasurementTypeCommand.SI,
              protocolUI);

            await UserActionHelper.RunWithUserRepeatAsync(async () =>
            {
              token.ThrowIfCancellationRequested();

              MeasurementRange measurementRange = new MeasurementRange(dataModel.Param, 1000, 60000);
              var answer = await breakDown.IrManger.Measure.MeasureAsync(ElectricalTestFunction.InsulationResistance, measurementRange);

              bool isSuccessful = answer.Value >= dataModel.Param;

              string? executionErrorMessage = !isSuccessful
                ? MeasurementMessages.BuildNodeFailure(
                  connectResult.PointModel,
                  dataModel.Param,
                  answer.Value,
                  ResistanceUnit.MegaOhm,
                  MeasurementLimitKind.Minimum)
                : null;
              await MeasurementMessages.PublishResultAsync(CheckType.Test,
                ResistanceUnit.MegaOhm,
                new MeasurementRange(answer.Value, dataModel.Param, -1),
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
