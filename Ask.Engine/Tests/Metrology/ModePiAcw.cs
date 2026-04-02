using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Device.Communication.Ethernet.Udp;
using Ask.Device.Runtime.Ethernet.Udp.Broadcast;
using Ask.Engine.Tests.Metrology.MeasurementSystem;
using static Ask.Engine.Tests.Base.UIValidationHelper;

namespace Ask.Engine.Tests.Metrology
{
  /// <summary>
  /// Реализует алгоритм выполнения метрологического контроля в режиме ПИ ACW.
  /// </summary>
  public class ModePiAcw : IExecution
  {
    /// <summary>
    /// Текущий метрологический режим - ПИ ACW.
    /// </summary>
    private MeasurementTypeCommand metrologicalModeRole => MeasurementTypeCommand.PI_ACW;

    /// <summary>
    /// Экземпляр объекта, инкапсулирующего логику проведения измерений и работу с оборудованием для данного режима.
    /// </summary>
    private PiMeasurement testMeasurement;

    /// <summary>
    /// Сервис взаимодействия с пользователем: вывод сообщений, запросы подтверждений, отображение результатов и ошибок.
    /// </summary>
    private IUserInteractionService _userInteractionService;

    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    public async void InitializeSettings(IExecutionController executionController, IUserInteractionService userInteractionService, IReferenceVoltageRequestService referenceVoltageRequestService)
    {
      _userInteractionService = userInteractionService;
      testMeasurement = new PiMeasurement(referenceVoltageRequestService);

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
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task ExecuteMeasurementProcess(IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var data = await EnsureValidMetrologyInputAsync(inputFieldProvider, _userInteractionService, timeCheck: true, timeRampCheck: true);
      await UdpBroadcastCommandSender.ResetAllDevicesAsync();

      await testMeasurement.ConnectToEquipment(data.FirstPoint, data.SecondPoint, metrologicalModeRole, _userInteractionService);
      await testMeasurement.SetupCommutation(_userInteractionService, data.FirstPoint, data.SecondPoint, metrologicalModeRole);
      await testMeasurement.ConfigureMeter(_userInteractionService, metrologicalModeRole, data);

      var (LowerBound, UpperBound, delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.PI_ACW, data.Param);
      await _userInteractionService.AppendEmptyLineAsync();
      await _userInteractionService.ShowMessageAsync(new ShowMessageModel("Диапазон допускаемых значений", headerColor: ShowMessageModel.SuccessMessage.TitleColor, message: $"от {LowerBound} до {UpperBound} В"));


      await UserActionHelper.RunWithUserRepeatAsync(async () => await testMeasurement.PerformMeasurement(metrologicalModeRole, data.Param, _userInteractionService), _userInteractionService, true);
    }

    public ITextAdapter GetControl()
    {
      return _userInteractionService;
    }

    private class PiMeasurement : BaseMeasurement
    {
      private IReferenceVoltageRequestService _reference;

      public PiMeasurement(IReferenceVoltageRequestService referenceVoltageRequestService) : base()
      {
        _reference = referenceVoltageRequestService;
      }

      /// <inheritdoc />
      public override async Task ConfigureMeter(IUserInteractionService messageService, MeasurementTypeCommand metrologicalModeRole, DataModel dataModel = null)
      {
        var breakDown = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IBreakdownTester>().FirstOrDefault() : null;
        string name = breakDown.Name;
        int chassis = breakDown.NumberChassis;
        int numer = breakDown.Number;

        await breakDown.ConnectableManager.InitializeAsync(messageService);
        await breakDown.AcwManger.Mode.SetModeAsync(messageService);
        await breakDown.AcwManger.Time.SetTestTimeAsync(dataModel.Time, messageService);
        await breakDown.AcwManger.Time.SetRampTimeAsync(dataModel.RampTime, messageService);
        await breakDown.AcwManger.FrequencyConfigurable.SetFrequencyAsync(50, messageService);
        await breakDown.AcwManger.CurrentLimits.SetLowCurrentLimitAsync(0, messageService);
        await breakDown.AcwManger.CurrentLimits.SetHighCurrentLimitAsync(40, messageService);
        await breakDown.AcwManger.Voltage.SetVoltageAsync(dataModel.Param, messageService);
      }

      /// <inheritdoc />
      public override async Task<bool> PerformMeasurement(MeasurementTypeCommand metrologicalModeRole, double param, IUserInteractionService userMessageService, double intrinsicValue = 0)
      {
        var meterDevice = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IBreakdownTester>().FirstOrDefault() : null;
        await userMessageService.ShowMessageAsync(new ShowMessageModel(header: "Выполнение измерения сопротивления изоляции", headerColor: ShowMessageModel.SuccessMessage.TitleColor));

        (LowerBound, UpperBound, var delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.PI_ACW, param);
        await meterDevice.AcwManger.Measure.MeasureAsync(param, LowerBound, UpperBound);
        var result = await MeasuredReferenceMeter(userMessageService, param);

        var answer = (result >= LowerBound && result <= UpperBound) ? false : true;
        var err = result - param;
        Measurements.Add(err);

        await userMessageService.ShowMessageAsync(new ShowMessageModel("Результат измерения напряжения", message: MeasurementValueFormatter.FormatWithUnit(result, "�"), type: result >= LowerBound && result <= UpperBound ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 1 }, skipPause: true);
        await userMessageService.ShowMessageAsync(new ShowMessageModel("Погрешность измерения", message: MeasurementValueFormatter.FormatWithUnit(err, "�"), type: result >= LowerBound && result <= UpperBound ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 2 }, skipPause: true);

        return true;
      }
      public override async Task FinalizeMeasurement(IUserInteractionService messageService)
      {
        await base.FinalizeMeasurement(messageService);
        await PrintResult(messageService, MeasurementTypeCommand.PI_ACW);
        await messageService.ShowMessageAsync(new ShowMessageModel("Диапазон допускаемых значений", headerColor: ShowMessageModel.SuccessMessage.TitleColor, message: $"от {LowerBound} до {UpperBound} В"));

        Measurements.Clear();
      }

      private async Task<double> MeasuredReferenceMeter(IUserInteractionService userMessageService, double param)
      {
        var result = await _reference.RequestReferenceVoltageAsync(userMessageService.GetControl());
        return result == null ? -1 : result.Value;
      }
    }
  }
}

