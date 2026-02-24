using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Engine.Tests.Base;
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
    private IFastMeter _fastMeter;

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

      _controller.SetSettings(
          StartDelegate: ExecuteTestProcess,
          true,
          StopDelegate: Stop);
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
          pairBusCheck: true);

      _pairBus = data.ActivePairBus;
      // МКР
      _module = RelayModuleHelper.GetModulesByRange
          (
              data.FirstPoint.DeviceNumber,
              data.FirstPoint.ModuleNumber,
              data.FirstPoint.ModuleNumber
          )
          .FirstOrDefault()
          ;

      // УКШ
      _busSwitcher = RelayModuleHelper.ResolveUksh(data.FirstPoint.DeviceNumber);

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

      // Переводим мультиметр в режим измерения сопротивления
      await RelayModuleHelper.EnsureResistanceModeAsync(
          _fastMeter,
          _userInteractionService,
          cancellationToken);

      await _userInteractionService.ShowMessageAsync(
          new ShowMessageModel("Инициализация завершена, тест начат!"),
          IsBlockStart: true);

      double result = 0;

      // Основной цикл теста
      for (int i = data.FirstPoint.PointNumber; i <= data.SecondPoint.PointNumber; i++)
      {
        // Коммутируем точку
        await RelayModuleHelper.PointConnectAsync(_module, BusPoint.AB, i, _userInteractionService, cancellationToken);

        // Измеряем сопротивление ПОСЛЕ коммутации точки
        result = await RelayModuleHelper.MeasureResistanceAsync(
            _fastMeter,
            _userInteractionService,
            cancellationToken,
            i,
            _module,
            data.Param);

        // Отключаем точку
        await RelayModuleHelper.PointDisconnectAsync(_module, BusPoint.AB, i, _userInteractionService, cancellationToken);
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
      if (!needReset) return;
      await RelayModuleHelper.ResetModule(_userInteractionService, _userInteractionService, _module);
      await RelayModuleHelper.DisconnectMultimeterFromBusAsync(_busSwitcher, _pairBus, _userInteractionService, cancellationToken);
      await RelayModuleHelper.ShutdownMeterAsync(_fastMeter, _userInteractionService, cancellationToken);
      await RelayModuleHelper.ShutdownUkshAsync(_busSwitcher, _userInteractionService, cancellationToken);
      needReset = false;
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