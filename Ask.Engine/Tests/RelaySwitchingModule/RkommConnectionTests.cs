using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Engine.Tests.Base;
using static Ask.Core.Shared.DTO.Protocol.ShowMessageModel;
using static Ask.Engine.Tests.Base.UIValidationHelper;

namespace Ask.Engine.Tests.RelaySwitchingModule
{
  public class RkommConnectionTests
  {

    private IExecutionController _controller;

    private IUserInteractionService _userInteractionService;

    private IMessageOutputService _messageOutputService;

    private IRelaySwitchModule _module;

    private ISwitchingDevice _busSwitcher;

    private IFastMeter _fastMeter;

    private SwitchingBusNew _pairBus;

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

      await _userInteractionService.ShowMessageAsync(
          new ShowMessageModel("Инициализация оборудования"),
          IsBlockStart: true);

      // Подключение к устройствам (МКР + УКШ + мультиметр)
      cancellationToken.ThrowIfCancellationRequested();
      await RelayModuleHelper.ConnectIfNeededAsync(_module, _userInteractionService, cancellationToken);

      cancellationToken.ThrowIfCancellationRequested();
      await RelayModuleHelper.ConnectIfNeededAsync(_busSwitcher, _userInteractionService, cancellationToken);

      cancellationToken.ThrowIfCancellationRequested();
      await RelayModuleHelper.ConnectIfNeededAsync(_fastMeter, _userInteractionService, cancellationToken);

      await _userInteractionService.ShowMessageAsync(
          new ShowMessageModel("Настройка оборудования"),
          IsBlockStart: true);

      // Подключаем МКР к выбранной паре шин
      await RelayModuleHelper.BusConnectAsync(
          ConvertingInSwitchingBusNewToSwitchingBus(data.ActivePairBus),
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

        // Измеряем сопротивление ДО коммутации точки
        //result = await RelayModuleHelper.MeasureResistanceAsync(
        //    _fastMeter,
        //    _userInteractionService,
        //    cancellationToken);

        //await _userInteractionService.ShowMessageAsync(new ShowMessageModel("Диапазон допускаемых значений", headerColor: SuccessMessage.TitleColor, message: $"Overload (невозможно оценить)"));
        //await _userInteractionService.ShowMessageAsync(new ShowMessageModel("Результат измерения сопротивления", message: $"{result} Ом", type: result >= data.Param ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 1 }, skipPause: true);
        //if (result >= data.Param)
        //{
        //    await _userInteractionService.ShowMessageAsync(
        //        new ShowMessageModel(
        //            $"Замыкание точки {i} отсутствует", 
        //            type: MessageType.Success)
        //        );
        //}
        //else
        //{
        //    await _userInteractionService.ShowMessageAsync(
        //        new ShowMessageModel(
        //            $"Замыкание точки {i} на шинах {ConvertingInSwitchingBusNewToString(data.ActivePairBus)}", 
        //            type: MessageType.Error)
        //        );
        //}

        // Коммутируем точку
        cancellationToken.ThrowIfCancellationRequested();
        await RelayModuleHelper.PointConnectAsync(_module, BusPoint.A, i, _userInteractionService, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        await RelayModuleHelper.PointConnectAsync(_module, BusPoint.B, i, _userInteractionService, cancellationToken);

        // Измеряем сопротивление ПОСЛЕ коммутации точки
        result = await RelayModuleHelper.MeasureResistanceAsync(
            _fastMeter,
            _userInteractionService,
            cancellationToken);

        //await _userInteractionService.ShowMessageAsync(new ShowMessageModel("Диапазон допускаемых значений", headerColor: SuccessMessage.TitleColor, message: $"до {data.Param} Ом"));
        await _userInteractionService.ShowMessageAsync(new ShowMessageModel($"Сопротивление точки {i}", message: $"{result} Ом", type: result <= data.Param ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 1 }, skipPause: true);

        //if (result <= data.Param)
        //{
        //    await _userInteractionService.ShowMessageAsync(
        //        new ShowMessageModel(
        //            $"Сопротивление точки {i} в пределах нормы",
        //            type: MessageType.Success)
        //        );
        //}
        //else
        //{
        //    await _userInteractionService.ShowMessageAsync(
        //        new ShowMessageModel(
        //            $"Сопротивление точки {i} выше нормы",
        //            type: MessageType.Error)
        //        );
        //}

        // Отключаем точку
        cancellationToken.ThrowIfCancellationRequested();
        await RelayModuleHelper.PointDisconnectAsync(_module, BusPoint.A, i, _userInteractionService, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        await RelayModuleHelper.PointDisconnectAsync(_module, BusPoint.B, i, _userInteractionService, cancellationToken);
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
      await RelayModuleHelper.ResetModule(_userInteractionService, _userInteractionService, _module);
      await RelayModuleHelper.DisconnectMultimeterFromBusAsync(_busSwitcher, _pairBus, _userInteractionService, cancellationToken);
      await RelayModuleHelper.ShutdownMeterAsync(_fastMeter, _userInteractionService, cancellationToken);
      await RelayModuleHelper.ShutdownUkshAsync(_busSwitcher, _userInteractionService, cancellationToken);
    }

    #region Вспомогательные методы

    /// <summary>
    /// Конвертация из <see cref="SwitchingBusNew"/> в <see cref="SwitchingBus"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Если какой-либо конвертации не оказалось здесь.</exception>
    private SwitchingBus ConvertingInSwitchingBusNewToSwitchingBus(SwitchingBusNew pairBus) =>
        pairBus switch
        {
          SwitchingBusNew.AB1 => SwitchingBus.AB1,
          SwitchingBusNew.AB2 => SwitchingBus.AB2,
          SwitchingBusNew.AB3 => SwitchingBus.AB3,
          SwitchingBusNew.AB4 => SwitchingBus.AB4,
          _ => throw new ArgumentOutOfRangeException(nameof(pairBus), $"Недопустимое значение для {nameof(SwitchingBusNew)}: {pairBus}"),
        };

    /// <summary>
    /// Конвертация из <see cref="SwitchingBusNew"/> в <see cref="string"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Если какой-либо конвертации не оказалось здесь.</exception>
    private string ConvertingInSwitchingBusNewToString(SwitchingBusNew pairBus) =>
        pairBus switch
        {
          SwitchingBusNew.AB1 => "А1В1",
          SwitchingBusNew.AB2 => "А2В2",
          SwitchingBusNew.AB3 => "А3В3",
          SwitchingBusNew.AB4 => "А4В4",
          _ => throw new ArgumentOutOfRangeException(nameof(pairBus), $"Недопустимое значение для {nameof(SwitchingBusNew)}: {pairBus}"),
        };

    #endregion

  }
}