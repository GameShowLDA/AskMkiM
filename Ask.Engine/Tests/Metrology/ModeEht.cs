using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Services.Errors.Device.Multimeter;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
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

      executionController.SetSettings(
        StartDelegate: ExecuteMeasurementProcess,
        true,
        StopDelegate: async (CancellationToken token) =>
        {
          await testMeasurement.FinalizeMeasurement(userInteractionService);
        });
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns></returns>
    private async Task ExecuteMeasurementProcess(IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, _userInteractionService);

      await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, metrologicalModeRole, _userInteractionService);
      await testMeasurement.SetupCommutation(_userInteractionService, data.FirstPoint, data.SecondPoint, metrologicalModeRole);
      await testMeasurement.ConfigureMeter(_userInteractionService, metrologicalModeRole);

      var (LowerBound, UpperBound, delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.EHT, data.Param);

      await _userInteractionService.AppendEmptyLineAsync();
      await _userInteractionService.ShowMessageAsync(new ShowMessageModel("Диапазон допускаемых значений", headerColor: ShowMessageModel.SuccessMessage.TitleColor, message: $"от {LowerBound} до {UpperBound} Ом"));

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
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;

        await fastMeter.ResistanceManager.SetResistanceModeAsync(messageService);
      }

      /// <inheritdoc />
      public override async Task<bool> PerformMeasurement(MeasurementTypeCommand metrologicalModeRole, double param, IUserInteractionService protocolUI)
      {
        var points = GetPoints();

        var Rt1 = await StepFirst(protocolUI, metrologicalModeRole, points.Point1, param);
        await protocolUI.ShowMessageAsync(new ShowMessageModel("Результат измерения сопротивления", message: $"{Rt1:F5} Ом") { IndentLevel = 1 });

        var Rt2 = await StepSecond(protocolUI, metrologicalModeRole, points.Point1, points.Point2, param);
        await protocolUI.ShowMessageAsync(new ShowMessageModel("Результат измерения сопротивления", message: $"{Rt2:F5} Ом") { IndentLevel = 1 });

        var Rt = await StepThird(protocolUI, metrologicalModeRole, points.Point1, points.Point2, param);
        await protocolUI.ShowMessageAsync(new ShowMessageModel("Результат измерения сопротивления", message: $"{Rt:F5} Ом") { IndentLevel = 1 });

        (LowerBound, UpperBound, var delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.EHT, param);
        var result = Rt - ((Rt1 + Rt2) / 2);

        var err = result - param;
        Measurements.Add(err);

        await protocolUI.ShowMessageAsync(new ShowMessageModel("Результат сопротивления", message: $"{result:F5} Ом", type: result >= LowerBound && result <= UpperBound ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 1 }, skipPause: true);
        await protocolUI.ShowMessageAsync(new ShowMessageModel("Погрешность измерения", message: $"{err:F5} Ом", type: result >= LowerBound && result <= UpperBound ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 2 }, skipPause: true);

        await StepReset(protocolUI, metrologicalModeRole, points.Point1, points.Point2, param);
        return true;
      }

      public override async Task FinalizeMeasurement(IUserInteractionService messageService)
      {
        await base.FinalizeMeasurement(messageService);
        await PrintResult(messageService, MeasurementTypeCommand.EHT);
        await messageService.ShowMessageAsync(new ShowMessageModel("Диапазон допускаемых значений", message: $"от {LowerBound} до {UpperBound} Ом") { IndentLevel = 1 }, skipPause: true);

        Measurements.Clear();
      }


      private async Task<double> StepFirst(IUserInteractionService userMessageService, MeasurementTypeCommand metrologicalModeRole, PointModel point1, double param)
      {
        await userMessageService.ShowMessageAsync(new ShowMessageModel(header: $"Подключение точки {point1}"), IsBlockStart: true);

        var relayModule = GetRelayModules(metrologicalModeRole).First();

        await relayModule.PointManager.ConnectRelayAsync(BusPoint.A, point1.PointNumber, userMessageService);
        await relayModule.PointManager.ConnectRelayAsync(BusPoint.B, point1.PointNumber, userMessageService);

        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;

        await userMessageService.ShowMessageAsync(new ShowMessageModel(header: $"Измерение сопротивления"), IsBlockStart: true);

        var result = !await ExecutionConfig.GetIsIdleModeEnabled() ? await fastMeter.ResistanceManager.MeasureResistanceAsync() : !await ExecutionConfig.GetIsErrorSimulationEnabled() ? param / 2 : new Random().Next((int)param - 100, (int)param + 100);
        return result;
      }


      private async Task<double> StepSecond(IUserInteractionService userMessageService, MeasurementTypeCommand metrologicalModeRole, PointModel point1, PointModel point2, double param)
      {
        await userMessageService.ShowMessageAsync(new ShowMessageModel(header: $"Отлючение точки {point1}"), IsBlockStart: true);
        var relayModule = GetRelayModules(metrologicalModeRole).First();

        await relayModule.PointManager.DisconnectRelayAsync(BusPoint.B, point1.PointNumber, userMessageService);
        relayModule = GetRelayModules(metrologicalModeRole).Last();

        await userMessageService.ShowMessageAsync(new ShowMessageModel(header: $"Подлючение точки {point2}"), IsBlockStart: true);
        
        await relayModule.PointManager.ConnectRelayAsync(BusPoint.A, point2.PointNumber, userMessageService);
        await relayModule.PointManager.ConnectRelayAsync(BusPoint.B, point2.PointNumber, userMessageService);
        
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;

        await userMessageService.ShowMessageAsync(new ShowMessageModel(header: $"Измерение сопротивления"), IsBlockStart: true);

        var result = !await ExecutionConfig.GetIsIdleModeEnabled() ? await fastMeter.ResistanceManager.MeasureResistanceAsync() : !await ExecutionConfig.GetIsErrorSimulationEnabled() ? param / 2 : new Random().Next((int)param - 100, (int)param + 100);
        return result;
      }

      private async Task<double> StepThird(IUserInteractionService userMessageService, MeasurementTypeCommand metrologicalModeRole, PointModel point1, PointModel point2, double param)
      {
        await userMessageService.ShowMessageAsync(new ShowMessageModel(header: $"Отлючение точки {point2}"), IsBlockStart: true);
        
        var relayModule = GetRelayModules(metrologicalModeRole).Last();
        await relayModule.PointManager.DisconnectRelayAsync(BusPoint.A, point2.PointNumber, userMessageService);

        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;

        await userMessageService.ShowMessageAsync(new ShowMessageModel(header: $"Измерение сопротивления"), IsBlockStart: true);
        var result = !await ExecutionConfig.GetIsIdleModeEnabled() ? await fastMeter.ResistanceManager.MeasureResistanceAsync() : !await ExecutionConfig.GetIsErrorSimulationEnabled() ? param * 1.5 : new Random().Next((int)param - 100, (int)param + 100);
        return result;
      }

      private async Task StepReset(IUserInteractionService userMessageService, MeasurementTypeCommand metrologicalModeRole, PointModel point1, PointModel point2, double param)
      {
        await userMessageService.ShowMessageAsync(new ShowMessageModel(header: $"Отлючение точек"), IsBlockStart: true);
        
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
