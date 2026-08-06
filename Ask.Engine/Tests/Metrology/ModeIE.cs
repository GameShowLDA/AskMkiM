using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
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
  /// Реализует алгоритм выполнения метрологического контроля в режиме ИЕ.
  /// </summary>
  public class ModeIE : IExecution
  {
    /// <summary>
    /// Текущий метрологический режим - ИЕ.
    /// </summary>
    private MeasurementTypeCommand metrologicalModeRole => MeasurementTypeCommand.IE;

    /// <summary>
    /// Экземпляр объекта, инкапсулирующего логику проведения измерений и работу с оборудованием для данного режима.
    /// </summary>
    private IeMeasurement testMeasurement = new IeMeasurement();

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
          await testMeasurement.FinalizeMeasurement(metrologicalModeRole, _userInteractionService);
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
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, _userInteractionService, metrologyMode: metrologicalModeRole);

      await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, metrologicalModeRole, _userInteractionService);
      await testMeasurement.SetupCommutation(_userInteractionService, data.FirstPoint, data.SecondPoint, metrologicalModeRole);
      await testMeasurement.ConfigureMeter(_userInteractionService, metrologicalModeRole);

      var (LowerBound, UpperBound, delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.IE, data.Param);

      await _userInteractionService.AppendEmptyLineAsync();
      await RangeMessages.PublishAllowedRangeAsync(
        CapacitanceUnit.NanoFarad,
        new MeasurementRange(LowerBound, LowerBound, UpperBound),
        _userInteractionService);

      var intrinsicCapacitance = testMeasurement.GetIntrinsicCapacitanceByPoints(data.FirstPoint, data.SecondPoint);
      await UserActionHelper.RunWithUserRepeatAsync(async () => await testMeasurement.PerformMeasurement(metrologicalModeRole, data.Param, _userInteractionService, intrinsicCapacitance), _userInteractionService, true);
    }

    public ITextAdapter GetControl()
    {
      return _userInteractionService;
    }

    private class IeMeasurement : BaseMeasurement
    {
      public IeMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(IUserInteractionService messageService, MeasurementTypeCommand metrologicalModeRole, DataModel dataModel = null)
      {
        await base.ConfigureMeter(messageService, metrologicalModeRole, dataModel);

        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IMultimeter>().FirstOrDefault() : null;
        await fastMeter.CapacitanceManager.SetCapacitanceModeAsync(messageService);
      }

      /// <inheritdoc />
      public override async Task<bool> PerformMeasurement(MeasurementTypeCommand metrologicalModeRole, double param, IUserInteractionService protocolUI, double intrinsicValue = 0)
      {
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IMultimeter>().FirstOrDefault() : null;
        await MeasurementMessages.PublishStartAsync(
          MeasurementTypeCommand.IE,
          protocolUI);
        (LowerBound, UpperBound, var delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.IE, param);

        MeasurementRange measurementRange = new MeasurementRange(param, LowerBound, UpperBound);
        double result = await fastMeter.CapacitanceManager.MeasureCapacitanceAsync(measurementRange, userMessageService: protocolUI);

        if (!ExecutionConfig.GetIsIdleModeEnabled() && result != 9.8999999999999969E+46)
        {
          result -= intrinsicValue;
        }

        var err = result - param;
        Measurements.Add(err);

        if (result != 9.8999999999999969E+46)
        {
          if (result < LowerBound || result > UpperBound)
          {
            AddMetrologyError(protocolUI, metrologicalModeRole, result, LowerBound, UpperBound, "нФ");
          }

          await MeasurementMessages.PublishResultAsync(MeasurementTypeCommand.IE, new MeasurementRange(result, LowerBound, UpperBound), result >= LowerBound && result <= UpperBound, outputService: protocolUI);
          await MeasurementMessages.PublishErrorAsync(
            CapacitanceUnit.NanoFarad,
            new MeasurementRange(err, LowerBound, UpperBound),
            result >= LowerBound && result <= UpperBound,
            protocolUI);
        }
        else
        {
          AddMetrologyError(protocolUI, metrologicalModeRole, "Overload", LowerBound, UpperBound, "нФ");
          await MeasurementMessages.PublishResultAsync(MeasurementTypeCommand.IE, new MeasurementRange(result, LowerBound, UpperBound), result >= LowerBound && result <= UpperBound, outputService: protocolUI);
        }
        return true;
      }

      /// <summary>
      /// Возвращает собственную ёмкость "старшего" модуля (с большим номером модуля)
      /// среди модулей, соответствующих заданным точкам.
      /// </summary>
      public double GetIntrinsicCapacitanceByPoints(PointModel point1, PointModel point2)
      {
        var relayModules = GetRelayModules(MeasurementTypeCommand.IE);
        if (relayModules == null || relayModules.Count == 0)
        {
          return 0;
        }

        var selectedModule = relayModules
          .Where(module =>
            (module.NumberChassis == point1.DeviceNumber && module.Number == point1.ModuleNumber) ||
            (module.NumberChassis == point2.DeviceNumber && module.Number == point2.ModuleNumber))
          .OrderByDescending(module => module.Number)
          .FirstOrDefault();

        return selectedModule?.SwitchCapacitance ?? 0;
      }

      public override async Task FinalizeMeasurement(MeasurementTypeCommand metrologicalModeRole, IUserInteractionService messageService)
      {
        await PrintResult(messageService, MeasurementTypeCommand.IE);
        await RangeMessages.PublishAllowedRangeAsync(
          CapacitanceUnit.NanoFarad,
          new MeasurementRange(LowerBound, LowerBound, UpperBound),
          messageService,
          indentLevel: 1);
        await base.FinalizeMeasurement(metrologicalModeRole, messageService);

        Measurements.Clear();
      }
    }
  }
}
