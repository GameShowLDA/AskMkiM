using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.Tests.Metrology.MeasurementSystem;
using static Ask.Engine.Tests.Base.UIValidationHelper;

namespace Ask.Engine.Tests.Metrology
{
  /// <summary>
  /// Реализует алгоритм выполнения метрологического контроля в режиме СИ.
  /// </summary>
  public class ModeCI : IExecution
  {
    /// <summary>
    /// Текущий метрологический режим - СИ.
    /// </summary>
    private MeasurementTypeCommand metrologicalModeRole => MeasurementTypeCommand.SI;

    /// <summary>
    /// Экземпляр объекта, инкапсулирующего логику проведения измерений и работу с оборудованием для данного режима.
    /// </summary>
    private CiMeasurement testMeasurement = new CiMeasurement();

    /// <summary>
    /// Сервис взаимодействия с пользователем: вывод сообщений, запросы подтверждений, отображение результатов и ошибок.
    /// </summary>
    private IUserInteractionService _userInteractionService;

    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    public void InitializeSettings(IExecutionController executionController, IUserInteractionService userInteractionService)
    {
      _userInteractionService = userInteractionService;
      testMeasurement.SetExecutionController(executionController);

      ActionSettings settings = new ActionSettings()
      {
        StartDelegate = ExecuteMeasurementProcess,
        CheckType = CheckType.Metrology,
        StopDelegate = async (CancellationToken token) =>
        {
          await testMeasurement.FinalizeMeasurement(metrologicalModeRole, userInteractionService);
        }
      };

      executionController.SetSettings(settings);
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns></returns>
    private async Task ExecuteMeasurementProcess(ActionSettings settings, IUserInteractionService messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, messageService, metrologyMode: metrologicalModeRole, timeCheck: true, voltageCheck: true);

      await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, metrologicalModeRole, messageService);
      await testMeasurement.SetupCommutation(messageService, data.FirstPoint, data.SecondPoint, metrologicalModeRole);
      await testMeasurement.ConfigureMeter(messageService, metrologicalModeRole, data);
      var (LowerBound, UpperBound, delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.SI, data.Param);

      await messageService.AppendEmptyLineAsync();
      await RangeMessages.PublishAllowedRangeAsync(
        ResistanceUnit.Ohm,
        new MeasurementRange(LowerBound, LowerBound, UpperBound),
        messageService);

      await UserActionHelper.RunWithUserRepeatAsync(async () => await testMeasurement.PerformMeasurement(metrologicalModeRole, data.Param, messageService), _userInteractionService, true);
    }

    /// <inheritdoc />
    public ITextAdapter GetControl()
    {
      return _userInteractionService;
    }

    private class CiMeasurement : BaseMeasurement
    {
      public CiMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(IUserInteractionService messageService, MeasurementTypeCommand metrologicalModeRole, DataModel dataModel = null)
      {
        await base.ConfigureMeter(messageService, metrologicalModeRole, dataModel);
        var breakDown = Devices.TryGetValue(MeasurementTypeCommand.SI, out var meter) ? meter.OfType<IBreakdownTester>().FirstOrDefault() : null;
        string name = breakDown.Name;
        int chassis = breakDown.NumberChassis;
        int numer = breakDown.Number;

        await breakDown.ConnectableManager.InitializeAsync(messageService);
        await breakDown.IrManger.Mode.SetModeAsync(messageService);
        await breakDown.IrManger.Voltage.SetVoltageAsync(dataModel.Voltage, messageService);
        await breakDown.IrManger.Time.SetTestTimeAsync(dataModel.Time, messageService);
      }

      /// <inheritdoc />
      public override async Task<bool> PerformMeasurement(MeasurementTypeCommand metrologicalModeRole, double param, IUserInteractionService protocolUI, double intrinsicValue = 0)
      {
        var meterDevice = Devices.TryGetValue(MeasurementTypeCommand.SI, out var meter) ? meter.OfType<IBreakdownTester>().FirstOrDefault() : null;
        await MeasurementMessages.PublishStartAsync(
          MeasurementTypeCommand.SI,
          protocolUI);
        (LowerBound, UpperBound, var delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.SI, param);

        MeasurementRange measurementRange = new MeasurementRange(param, LowerBound, UpperBound);
        var result = (await meterDevice.IrManger.Measure.MeasureAsync(ElectricalTestFunction.InsulationResistance, measurementRange, userMessageService: protocolUI)).value;

        if (!ExecutionConfig.GetIsIdleModeEnabled())
        {
          result -= intrinsicValue;
        }

        var err = result - param;
        Measurements.Add(err);

        if (result < LowerBound || result > UpperBound)
        {
          AddMetrologyError(protocolUI, metrologicalModeRole, result, LowerBound, UpperBound, "МОм");
        }

        await MeasurementMessages.PublishResultAsync(MeasurementTypeCommand.SI, new MeasurementRange(result, LowerBound, UpperBound), result >= LowerBound && result <= UpperBound, outputService: protocolUI);
        await MeasurementMessages.PublishErrorAsync(
          ResistanceUnit.MegaOhm,
          new MeasurementRange(err, LowerBound, UpperBound),
          result >= LowerBound && result <= UpperBound,
          protocolUI);

        return true;
      }

      /// <inheritdoc />
      public override async Task FinalizeMeasurement(MeasurementTypeCommand metrologicalModeRole, IUserInteractionService messageService)
      {
        await PrintResult(messageService, MeasurementTypeCommand.SI);
        await RangeMessages.PublishAllowedRangeAsync(
          ResistanceUnit.Ohm,
          new MeasurementRange(LowerBound, LowerBound, UpperBound),
          messageService,
          indentLevel: 1);
        await base.FinalizeMeasurement(metrologicalModeRole, messageService);
        Measurements.Clear();
      }
    }
  }
}
