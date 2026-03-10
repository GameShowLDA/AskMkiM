
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static;
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

      executionController.SetSettings(
          StartDelegate: ExecuteMeasurementProcess,
          true,
          StopDelegate: async (CancellationToken token) =>
          {
            await testMeasurement.FinalizeMeasurement(_userInteractionService);
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
      await NewCore.Communication.DeviceCommandSender.ResetAllSystem();

      await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, metrologicalModeRole, _userInteractionService);
      await testMeasurement.SetupCommutation(_userInteractionService, data.FirstPoint, data.SecondPoint, metrologicalModeRole);
      await testMeasurement.ConfigureMeter(_userInteractionService, metrologicalModeRole);

      var (firstNorm, lastNorm, delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.PR, data.Param);

      await _userInteractionService.AppendEmptyLineAsync();
      await _userInteractionService.ShowMessageAsync(new ShowMessageModel("Диапазон допускаемых значений", headerColor: ShowMessageModel.SuccessMessage.TitleColor, message: $"от {firstNorm} до {lastNorm} Ом"));

      await UserActionHelper.RunWithUserRepeatAsync(async () => await testMeasurement.PerformMeasurement(metrologicalModeRole, data.Param, _userInteractionService), _userInteractionService, true);
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

        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;
        await fastMeter.ContinuityManager.SetContinuityModeAsync(messageService);
      }

      /// <inheritdoc />
      public override async Task<bool> PerformMeasurement(MeasurementTypeCommand metrologicalModeRole, double param, IUserInteractionService protocolUI, double intrinsicValue = 0)
      {
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;

        await protocolUI.ShowMessageAsync(new ShowMessageModel(header: "Выполнение измерения сопротивления"), IsBlockStart: true);
        var (firstNorm, lastNorm, delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.PR, param);

        var result = await fastMeter.ContinuityManager.CheckContinuityAsync(param, firstNorm, lastNorm);

        if (!ExecutionConfig.GetIsIdleModeEnabled())
        {
          result -= intrinsicValue;
        }

        var err = result - param;
        Measurements.Add(err);

        await protocolUI.ShowMessageAsync(new ShowMessageModel("Результат измерения сопротивления", message: $"{result} Ом", type: result >= firstNorm && result <= lastNorm ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 1 }, skipPause: true);
        await protocolUI.ShowMessageAsync(new ShowMessageModel("Погрешность измерения", message: $"{err} Ом", type: result >= firstNorm && result <= lastNorm ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 2 }, skipPause: true);

        return true;
      }

      public override async Task FinalizeMeasurement(IUserInteractionService messageService)
      {
        await base.FinalizeMeasurement(messageService);
        await PrintResult(messageService, MeasurementTypeCommand.PR);
        await messageService.ShowMessageAsync(new ShowMessageModel("Диапазон допускаемых значений", message: $"от {LowerBound} до {UpperBound} Ом") { IndentLevel = 1 }, skipPause: true);

        Measurements.Clear();
      }
    }
  }
}
