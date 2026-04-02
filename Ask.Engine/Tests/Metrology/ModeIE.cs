using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.Tests.Metrology.MeasurementSystem;
using Newtonsoft.Json.Linq;
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
    private MeasurementTypeCommand MetrologicalModeRole => MeasurementTypeCommand.IE;

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

      executionController.SetSettings(
        StartDelegate: ExecuteMeasurementProcess,
        true);
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task ExecuteMeasurementProcess(IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, _userInteractionService);

      await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, MetrologicalModeRole, _userInteractionService);
      await testMeasurement.SetupCommutation(_userInteractionService, data.FirstPoint, data.SecondPoint, MetrologicalModeRole);
      await testMeasurement.ConfigureMeter(_userInteractionService, MetrologicalModeRole);

      var (LowerBound, UpperBound, delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.IE, data.Param);

      await _userInteractionService.AppendEmptyLineAsync();
      await _userInteractionService.ShowMessageAsync(new ShowMessageModel("Диапазон допускаемых значений", headerColor: ShowMessageModel.SuccessMessage.TitleColor, message: $"от {LowerBound} до {UpperBound} нФ"));

      var intrinsicCapacitance = testMeasurement.GetIntrinsicCapacitanceByPoints(data.FirstPoint, data.SecondPoint);
      await UserActionHelper.RunWithUserRepeatAsync(async () => await testMeasurement.PerformMeasurement(MetrologicalModeRole, data.Param, _userInteractionService, intrinsicCapacitance), _userInteractionService, true);
      await testMeasurement.FinalizeMeasurement(_userInteractionService);
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

        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;
        await fastMeter.CapacitanceManager.SetCapacitanceModeAsync(messageService);
      }

      /// <inheritdoc />
      public override async Task<bool> PerformMeasurement(MeasurementTypeCommand metrologicalModeRole, double param, IUserInteractionService protocolUI, double intrinsicValue = 0)
      {
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;
        await protocolUI.ShowMessageAsync(new ShowMessageModel(header: "Выполнение измерения ёмкости"));
        (LowerBound, UpperBound, var delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.IE, param);

        double result = 0;
        List<double> measuremend = new List<double>();

        for (int i = 0; i < 6; i++)
        {
          result = await fastMeter.CapacitanceManager.MeasureCapacitanceAsync(param, LowerBound, UpperBound, protocolUI);
          if (result > 0)
          {
            measuremend.Add(result);
          }
        }
        result = measuremend.Average();

        if (!ExecutionConfig.GetIsIdleModeEnabled() && result != 9.8999999999999969E+46)
        {
          result -= intrinsicValue;
        }

        var err = result - param;
        Measurements.Add(err);

        if (result != 9.8999999999999969E+46)
        {
          await protocolUI.ShowMessageAsync(new ShowMessageModel("Результат измерения ёмкости", message: MeasurementValueFormatter.FormatWithUnit(result, "нФ"), type: result >= LowerBound && result <= UpperBound ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 1 }, skipPause: true);
          await protocolUI.ShowMessageAsync(new ShowMessageModel("Погрешность измерения", message: MeasurementValueFormatter.FormatWithUnit(err, "нФ"), type: result >= LowerBound && result <= UpperBound ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 2 }, skipPause: true);
        }
        else
        {
          await protocolUI.ShowMessageAsync(new ShowMessageModel("Результат измерения ёмкости", message: $"Overload", type: result >= LowerBound && result <= UpperBound ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 1 }, skipPause: true);
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

      public override async Task FinalizeMeasurement(IUserInteractionService messageService)
      {
        await base.FinalizeMeasurement(messageService);
        await PrintResult(messageService, MeasurementTypeCommand.IE);
        await messageService.ShowMessageAsync(new ShowMessageModel("Диапазон допускаемых значений", message: $"от {LowerBound} до {UpperBound} нФ") { IndentLevel = 1 }, skipPause: true);

        Measurements.Clear();
      }
    }
  }
}
