
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.Measurements;
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
  /// Реализует алгоритм выполнения метрологического контроля в режиме ПР.
  /// </summary>
  public class ModePr
  {
    /// <summary>
    /// Текущий метрологический режим - ПР.
    /// </summary>
    private MeasurementTypeCommand metrologicalModeRole => MeasurementTypeCommand.PR;

    /// <summary>
    /// Экземпляр объекта, инкапсулирующего логику проведения измерений и работу с оборудованием для данного режима.
    /// </summary>
    private PrMeasurement testMeasurement = new PrMeasurement();

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
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns></returns>
    private async Task ExecuteMeasurementProcess(IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, _userInteractionService, metrologyMode: metrologicalModeRole);
      await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, metrologicalModeRole, _userInteractionService);
      await testMeasurement.SetupCommutation(_userInteractionService, data.FirstPoint, data.SecondPoint, metrologicalModeRole);
      await testMeasurement.ConfigureMeter(_userInteractionService, metrologicalModeRole);

      var (firstNorm, lastNorm, delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.PR, data.Param);

      await _userInteractionService.AppendEmptyLineAsync();
      var realyModule = testMeasurement.GetRelayModuleWithMaxNumber(metrologicalModeRole);
      await UserActionHelper.RunWithUserRepeatAsync(async () => await testMeasurement.PerformMeasurement(metrologicalModeRole, data.Param, _userInteractionService, realyModule.SwitchResistance), _userInteractionService, true);
    }

    public ITextAdapter GetControl()
    {
      return _userInteractionService;
    }

    private class PrMeasurement : BaseMeasurement
    {
      public PrMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(IUserInteractionService messageService, MeasurementTypeCommand metrologicalModeRole, DataModel dataModel = null)
      {
        await base.ConfigureMeter(messageService, metrologicalModeRole, dataModel);

        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IMultimeter>().FirstOrDefault() : null;
        await fastMeter.ContinuityManager.SetContinuityModeAsync(messageService);
      }

      /// <inheritdoc />
      public override async Task<bool> PerformMeasurement(MeasurementTypeCommand metrologicalModeRole, double param, IUserInteractionService protocolUI, double intrinsicValue = 0)
      {
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IMultimeter>().FirstOrDefault() : null;

        await MeasurementMessages.PublishStartAsync(CheckType.Metrology,
          MeasurementTypeCommand.PR,
          protocolUI,
          isBlockStart: true);
        var (firstNorm, lastNorm, delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.PR, param);

        MeasurementRange measurementRange = new MeasurementRange(param, firstNorm, lastNorm);
        var result = await fastMeter.ContinuityManager.CheckContinuityAsync(measurementRange, protocolUI);

        if (!ExecutionConfig.GetIsIdleModeEnabled())
        {
          result -= intrinsicValue;
        }

        var err = result - param;
        Measurements.Add(err);

        if (result < firstNorm || result > lastNorm)
        {
          AddMetrologyError(protocolUI, metrologicalModeRole, result, firstNorm, lastNorm, "Ом");
        }

        await MeasurementMessages.PublishResultAsync(CheckType.Metrology, MeasurementTypeCommand.PR, new MeasurementRange(result, firstNorm, lastNorm), result >= firstNorm && result <= lastNorm, chains: MeasurementPointsDisplay, outputService: protocolUI);
        await PublishMetrologyMeasurementErrorAsync(
          ResistanceUnit.Ohm,
          new MeasurementRange(err, firstNorm, lastNorm),
          result >= firstNorm && result <= lastNorm,
          protocolUI);

        return true;
      }

      public override async Task FinalizeMeasurement(MeasurementTypeCommand metrologicalModeRole, IUserInteractionService messageService)
      {
        await PrintResult(messageService, MeasurementTypeCommand.PR);
        await base.FinalizeMeasurement(metrologicalModeRole, messageService);

        Measurements.Clear();
      }
    }
  }
}
