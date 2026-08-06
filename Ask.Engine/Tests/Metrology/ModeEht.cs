using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static;
using Ask.Engine.Tests.Metrology.MeasurementSystem;
using static Ask.Engine.Tests.Base.UIValidationHelper;

namespace Ask.Engine.Tests.Metrology
{
  /// <summary>
  /// Реализует алгоритм выполнения метрологического контроля в режиме ЭТ.
  /// </summary>
  public class ModeEht : IExecution
  {
    /// <summary>
    /// Текущий метрологический режим - ЭТ.
    /// </summary>
    private MeasurementTypeCommand metrologicalModeRole => MeasurementTypeCommand.EHT;

    /// <summary>
    /// Экземпляр объекта, инкапсулирующего логику проведения измерений и работу с оборудованием для данного режима.
    /// </summary>
    private EhtMeasurement testMeasurement = new EhtMeasurement();

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
    private async Task ExecuteMeasurementProcess(IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, _userInteractionService, metrologyMode: metrologicalModeRole);

      await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, metrologicalModeRole, _userInteractionService);
      await testMeasurement.SetupCommutation(_userInteractionService, data.FirstPoint, data.SecondPoint, metrologicalModeRole);
      await testMeasurement.ConfigureMeter(_userInteractionService, metrologicalModeRole);

      var (LowerBound, UpperBound, delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.EHT, data.Param);

      await _userInteractionService.AppendEmptyLineAsync();
      await RangeMessages.PublishAllowedRangeAsync(
        ResistanceUnit.Ohm,
        new MeasurementRange(LowerBound, LowerBound, UpperBound),
        _userInteractionService);

      await UserActionHelper.RunWithUserRepeatAsync(async () => await testMeasurement.PerformMeasurement(metrologicalModeRole, data.Param, _userInteractionService), _userInteractionService, true);
    }

    public ITextAdapter GetControl()
    {
      return _userInteractionService;
    }

    private class EhtMeasurement : BaseMeasurement
    {

      public EhtMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(IUserInteractionService messageService, MeasurementTypeCommand metrologicalModeRole, DataModel dataModel = null)
      {
        await base.ConfigureMeter(messageService, metrologicalModeRole, dataModel);
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IMultimeter>().FirstOrDefault() : null;

        await fastMeter.ResistanceManager.SetResistanceModeAsync(messageService);
      }

      /// <inheritdoc />
      public override async Task<bool> PerformMeasurement(MeasurementTypeCommand metrologicalModeRole, double param, IUserInteractionService protocolUI, double intrinsicValue = 0)
      {
        var points = GetPoints();
        (LowerBound, UpperBound, var delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.EHT, param);
        MeasurementRange measurementRange = new MeasurementRange(param, LowerBound, UpperBound);

        var Rt1 = await StepFirst(protocolUI, metrologicalModeRole, points.Point1, measurementRange);
        if (DeviceDisplayConfig.GetIntermediateMeasurementResultsVisibility() || Rt1 > 100)
        {
          await MeasurementMessages.PublishStartAsync(
            MeasurementTypeCommand.EHT,
            protocolUI,
            isBlockStart: true);
          await MeasurementMessages.PublishIntermediateResultAsync(MeasurementTypeCommand.EHT, new MeasurementRange(Rt1, LowerBound, UpperBound), true, outputService: protocolUI);
        }

        var Rt2 = await StepSecond(protocolUI, metrologicalModeRole, points.Point1, points.Point2, measurementRange);
        if (DeviceDisplayConfig.GetIntermediateMeasurementResultsVisibility() || Rt2 > 100)
        {
          await MeasurementMessages.PublishStartAsync(
            MeasurementTypeCommand.EHT,
            protocolUI,
            isBlockStart: true);
          await MeasurementMessages.PublishIntermediateResultAsync(MeasurementTypeCommand.EHT, new MeasurementRange(Rt2, LowerBound, UpperBound), true, outputService: protocolUI);
        }

        var Rt = await StepThird(protocolUI, metrologicalModeRole, points.Point1, points.Point2, measurementRange);
        if (DeviceDisplayConfig.GetIntermediateMeasurementResultsVisibility() || Rt > 100)
        {
          await MeasurementMessages.PublishStartAsync(
            MeasurementTypeCommand.EHT,
            protocolUI,
            isBlockStart: true);
          await MeasurementMessages.PublishIntermediateResultAsync(MeasurementTypeCommand.EHT, new MeasurementRange(Rt, LowerBound, UpperBound), true, outputService: protocolUI);
        }

        var result = Rt - ((Rt1 + Rt2) / 2);
        if (ExecutionConfig.GetIsIdleModeEnabled() && !ExecutionConfig.GetIsErrorSimulationEnabled())
        {
          result = param;
        }

        var err = result - param;
        Measurements.Add(err);

        if (result < LowerBound || result > UpperBound)
        {
          AddMetrologyError(protocolUI, metrologicalModeRole, result, LowerBound, UpperBound, "Ом");
        }

        await MeasurementMessages.PublishResultAsync(MeasurementTypeCommand.EHT, new MeasurementRange(result, LowerBound, UpperBound), result >= LowerBound && result <= UpperBound, outputService: protocolUI);
        await MeasurementMessages.PublishErrorAsync(
          ResistanceUnit.Ohm,
          new MeasurementRange(err, LowerBound, UpperBound),
          result >= LowerBound && result <= UpperBound,
          protocolUI);

        await StepReset(protocolUI, metrologicalModeRole, points.Point1, points.Point2);
        return true;
      }

      public override async Task FinalizeMeasurement(MeasurementTypeCommand metrologicalModeRole, IUserInteractionService messageService)
      {
        await PrintResult(messageService, MeasurementTypeCommand.EHT);
        await RangeMessages.PublishAllowedRangeAsync(
          ResistanceUnit.Ohm,
          new MeasurementRange(LowerBound, LowerBound, UpperBound),
          messageService,
          indentLevel: 1);
        await base.FinalizeMeasurement(metrologicalModeRole, messageService);

        Measurements.Clear();
      }

      private async Task<double> StepFirst(IUserInteractionService userMessageService, MeasurementTypeCommand metrologicalModeRole, PointModel point1, MeasurementRange measurementRange)
      {
        await ExecutionMessages.PublishPointConnectionAsync(point1, userMessageService);

        var relayModule = GetRelayModules(metrologicalModeRole).First();

        await relayModule.PointManager.ConnectRelayAsync(BusPoint.A, point1.PointNumber, userMessageService);
        await relayModule.PointManager.ConnectRelayAsync(BusPoint.B, point1.PointNumber, userMessageService);

        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IMultimeter>().FirstOrDefault() : null;

        var result = await fastMeter.ResistanceManager.MeasureResistanceAsync(measurementRange, userMessageService);
        return result;
      }

      private async Task<double> StepSecond(IUserInteractionService userMessageService, MeasurementTypeCommand metrologicalModeRole, PointModel point1, PointModel point2, MeasurementRange measurementRange)
      {
        await ExecutionMessages.PublishPointDisconnectionAsync(point1, userMessageService);

        var relayModule = GetRelayModules(metrologicalModeRole).First();

        await relayModule.PointManager.DisconnectRelayAsync(BusPoint.B, point1.PointNumber, userMessageService);
        relayModule = GetRelayModules(metrologicalModeRole).Last();

        await ExecutionMessages.PublishPointConnectionAsync(point2, userMessageService);

        await relayModule.PointManager.ConnectRelayAsync(BusPoint.A, point2.PointNumber, userMessageService);
        await relayModule.PointManager.ConnectRelayAsync(BusPoint.B, point2.PointNumber, userMessageService);

        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IMultimeter>().FirstOrDefault() : null;

        var result = await fastMeter.ResistanceManager.MeasureResistanceAsync(measurementRange, userMessageService);
        return result;
      }

      private async Task<double> StepThird(IUserInteractionService userMessageService, MeasurementTypeCommand metrologicalModeRole, PointModel point1, PointModel point2, MeasurementRange measurementRange)
      {
        await ExecutionMessages.PublishPointDisconnectionAsync(point2, userMessageService);

        var relayModule = GetRelayModules(metrologicalModeRole).Last();
        await relayModule.PointManager.DisconnectRelayAsync(BusPoint.A, point2.PointNumber, userMessageService);

        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IMultimeter>().FirstOrDefault() : null;
        var result = await fastMeter.ResistanceManager.MeasureResistanceAsync(measurementRange, userMessageService);
        return result;
      }

      private async Task StepReset(IUserInteractionService userMessageService, MeasurementTypeCommand metrologicalModeRole, PointModel point1, PointModel point2)
      {
        await ExecutionMessages.PublishPointsDisconnectionAsync(userMessageService);

        var relayModule = GetRelayModules(metrologicalModeRole).First();
        await relayModule.PointManager.DisconnectRelayAsync(BusPoint.A, point1.PointNumber, userMessageService);

        relayModule = GetRelayModules(metrologicalModeRole).Last();
        await relayModule.PointManager.DisconnectRelayAsync(BusPoint.B, point2.PointNumber, userMessageService);
      }

      public override async Task ConnectRelayPointsAsync(List<IRelaySwitchModule> relayModules, PointModel point1, PointModel point2, IUserInteractionService protocolUI)
      {
        return;
      }
    }
  }
}
