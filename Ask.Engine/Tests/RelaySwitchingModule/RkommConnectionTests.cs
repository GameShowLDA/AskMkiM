using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Engine.Tests.Base;
using Ask.Engine.Tests.NodeMethod;
using static Ask.Engine.Tests.Base.UIValidationHelper;

namespace Ask.Engine.Tests.RelaySwitchingModule
{
  public class RkommConnectionTests
  {
    /// <summary>
    /// Устанавливает основные настройки выполнения теста.
    /// </summary>
    private IExecutionController _controller;

    /// <summary>
    /// Интерфейс для управления UI для взаимодействия с пользователем.
    /// </summary>
    private IUserInteractionService _userInteractionService;

    /// <summary>
    /// Отображение сообщений пользователю.
    /// </summary>
    private IMessageOutputService _messageOutputService;

    /// <summary>
    /// Интерфейс для управления модулем коммутации реле (МКР).
    /// </summary>
    private IRelaySwitchModule _module;

    /// <summary>
    /// Интерфейс для управления устройством коммутации шин (УКШ).
    /// </summary>
    private ISwitchingDevice _busSwitcher;

    /// <summary>
    /// Интерфейс для управления мультиметром.
    /// </summary>
    private IMultimeter _fastMeter;

    /// <summary>
    /// Коммутационная пара шин.
    /// </summary>
    private SwitchingBusNew _pairBus;

    /// <summary>
    /// Флаг необходимости сброса состояния оборудования при остановке теста.
    /// </summary>
    private bool needReset = false;

    /// <summary>
    /// Асинхронная настройка UI, добавление полей, запуск ProtocolSelfCheckControl.
    /// </summary>
    public async Task InitializeSettingsAsync(IExecutionController executionController, IUserInteractionService userInteractionService, IMessageOutputService messageOutputService)
    {
      _controller = executionController;
      _userInteractionService = userInteractionService;
      _messageOutputService = messageOutputService;

      ActionSettings settings = new ActionSettings()
      {
        StartDelegate = ExecuteTestProcess,
        CheckType = CheckType.Test,
        AccumulateErrorMessages = true,
        StopDelegate = Stop
      };

      _controller.SetSettings(settings);
    }

    /// <summary>
    /// Подготовка и основная логика теста.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task ExecuteTestProcess(
        IUserInteractionService _messageService,
        IInputFieldProvider inputFieldProvider,
        IInputHighlightService inputHighlightService,
        CancellationToken cancellationToken)
    {
      DataModel data = await EnsureValidMetrologyInputAsync(
          inputFieldProvider,
          _messageOutputService,
          metrologyMode: MeasurementTypeCommand.KC,
          pairBusCheck: true);

      _pairBus = data.ActivePairBus;
      // МКР
      _module = RelayModuleHelper.GetModulesByRangeAsync
          (data.FirstPoint.DeviceNumber,
           data.FirstPoint.ModuleNumber,
           data.FirstPoint.ModuleNumber).GetAwaiter().
          GetResult().
          FirstOrDefault()
          ;

      // УКШ
      _busSwitcher = await RelayModuleHelper.ResolveUkshAsync(data.FirstPoint.DeviceNumber);

      // Мультиметр
      _fastMeter = RelayModuleHelper.ResolveFastMeter(data.FirstPoint.DeviceNumber);

      needReset = true;

      await _userInteractionService.ShowMessageAsync(
          new ShowMessageModel("Инициализация оборудования"),
          IsBlockStart: true);

      // Подключение к устройствам (МКР + УКШ + мультиметр)
      await RelayModuleHelper.ConnectIfNeededAsync(_module, _userInteractionService, cancellationToken);
      await RelayModuleHelper.ConnectIfNeededAsync(_busSwitcher, _userInteractionService, cancellationToken);
      await RelayModuleHelper.ConnectIfNeededAsync(_fastMeter, _userInteractionService, cancellationToken);

      await _userInteractionService.ShowMessageAsync(
          new ShowMessageModel("Настройка оборудования"),
          IsBlockStart: true);

      var busses = ConvertingInSwitchingBusNewToSwitchingBus(data.ActivePairBus);

      // Подключаем МКР к выбранной паре шин
      await RelayModuleHelper.BusConnectAsync(busses.Item1,
          _module,
          _userInteractionService,
          cancellationToken);

      // Подключаем МКР к выбранной паре шин
      await RelayModuleHelper.BusConnectAsync(busses.Item2,
          _module,
          _userInteractionService,
          cancellationToken);

      // УКШ подключает мультиметр к этой же паре шин
      await RelayModuleHelper.ConnectMultimeterToBusAsync(
          _busSwitcher,
          data.ActivePairBus,
          _userInteractionService,
          cancellationToken);

      // Переводим мультиметр в режим прозвонки
      await RelayModuleHelper.EnsureResistanceModeAsync(
          _fastMeter,
          _userInteractionService,
          cancellationToken);

      await _userInteractionService.ShowMessageAsync(
          new ShowMessageModel("Инициализация завершена, тест начат!"),
          IsBlockStart: true);

      for (int i = data.FirstPoint.PointNumber; i <= data.SecondPoint.PointNumber; i++)
      {
        cancellationToken.ThrowIfCancellationRequested();

        await MeasurePointResistanceWithUserActionAsync(i, data.Param, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
      }
    }

    /// <summary>
    /// Выполняет измерение точки.
    /// </summary>
    private async Task MeasurePointResistanceWithUserActionAsync(
      int pointNumber,
      double expectedResistance,
      CancellationToken cancellationToken)
    {
      await UserActionHelper.RunWithUserRepeatAsync(
        () => MeasurePointResistanceAsync(pointNumber, expectedResistance, cancellationToken),
        _userInteractionService);
    }

    /// <summary>
    /// Подключает точку, измеряет сопротивление и возвращает признак успешного попадания в допуск.
    /// </summary>
    private async Task<bool> MeasurePointResistanceAsync(
      int pointNumber,
      double expectedResistance,
      CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      await _module.PointManager.ConnectRelayAsync(BusPoint.AB, pointNumber, _userInteractionService);

      try
      {
        const double lowerLimit = 0;
        MeasurementRange measurementRange = new MeasurementRange(
          expectedResistance,
          lowerLimit,
          1000000000);
        var (success, result) = await RelayModuleHelper.MeasureResistanceAsync(
            _fastMeter,
            null!,
            cancellationToken,
            pointNumber,
            _module,
            measurementRange);

        var point = new PointModel
        {
          DeviceNumber = _module.NumberChassis,
          ModuleNumber = _module.Number,
          PointNumber = pointNumber,
        };
        var type = success
          ? ShowMessageModel.MessageType.Success
          : ShowMessageModel.MessageType.Error;
        var resultMessage = new ShowMessageModel(
          $"Результат измерения точки {point}",
          message: NodeMethodProtocolBuilder.FormatValue(result, ResistanceUnit.Ohm),
          type: type)
        {
          IndentLevel = 2,
          ExecutionErrorMessage = success
            ? null
            : NodeMethodProtocolBuilder.BuildRangeFailure(
              point,
              lowerLimit,
              expectedResistance,
              result,
              ResistanceUnit.Ohm),
        };
        await _userInteractionService.ShowMessageAsync(resultMessage, skipPause: true);

        return success;
      }
      finally
      {
        await _module.PointManager.DisconnectRelayAsync(BusPoint.AB, pointNumber, _userInteractionService);
      }
    }

    /// <summary>
    /// Принудительно останавливает выполнение теста RKOMM:
    ///  • выключает измеритель;
    ///  • выключает УКШ;
    ///  • сбрасывает модуль.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task Stop(CancellationToken cancellationToken)
    {
      if (!needReset)
      {
        return;
      }

      try
      {
        await RelayModuleHelper.DisconnectMultimeterFromBusAsync(_busSwitcher, _pairBus, _userInteractionService, cancellationToken);
        await RelayModuleHelper.ShutdownMeterAsync(_fastMeter, _userInteractionService, cancellationToken);
        await RelayModuleHelper.ShutdownUkshAsync(_busSwitcher, _userInteractionService, cancellationToken);
      }
      finally
      {
        needReset = false;
      }
    }

    #region Вспомогательные методы

    /// <summary>
    /// Конвертация из <see cref="SwitchingBusNew"/> в <see cref="SwitchingBus"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Если какой-либо конвертации не оказалось здесь.</exception>
    private (SwitchingBus, SwitchingBus) ConvertingInSwitchingBusNewToSwitchingBus(SwitchingBusNew pairBus) =>
        pairBus switch
        {
          SwitchingBusNew.AB1 => (SwitchingBus.A1, SwitchingBus.B1),
          SwitchingBusNew.AB2 => (SwitchingBus.A2, SwitchingBus.B2),
          SwitchingBusNew.AB3 => (SwitchingBus.A3, SwitchingBus.B3),
          SwitchingBusNew.AB4 => (SwitchingBus.A4, SwitchingBus.B4),
          _ => throw new ArgumentOutOfRangeException(nameof(pairBus), $"Недопустимое значение для {nameof(SwitchingBusNew)}: {pairBus}"),
        };

    #endregion

  }
}
