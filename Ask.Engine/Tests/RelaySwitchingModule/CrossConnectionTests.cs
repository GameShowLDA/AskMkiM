using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Engine.Tests.Base;
using DataBaseConfiguration.Services.Device;
using static Ask.Core.Shared.DTO.Protocol.ShowMessageModel;
using static Ask.LogLib.LoggerUtility;


namespace Ask.Engine.Tests.RelaySwitchingModule
{
  public class CrossConnectionTests
  {
    /// <summary>
    /// Поле для общения с тестируемым БК
    /// </summary>
    private IRelaySwitchModule testedModuleRelayControl;

    /// <summary>
    /// Поле для общения с проверяющим БК
    /// </summary>
    private IRelaySwitchModule verificatModuleRelayControl;

    /// <summary>
    /// Флаг, указывающий на необходимость сброса модулей и системы после теста.
    /// </summary>
    private bool needReset = false;

    private IExecutionController _controller;

    private IUserInteractionService _userInteractionService;

    /// <summary>
    /// Асинхронная настройка UI, добавление полей, запуск ProtocolSelfCheckControl.
    /// </summary>
    public async Task InitializeSettingsAsync(IExecutionController executionController, IUserInteractionService userInteractionService)
    {
      _controller = executionController;
      _userInteractionService = userInteractionService;

      _controller.SetSettings(
        StartDelegate: ExecuteTestProcess,
        true,
        StopDelegate: Stop);
    }

    /// <summary>
    /// Ищет релейные модули по строкам "шасси.модуль" и сохраняет их в поля
    /// testedModuleRelayControl и verificatModuleRelayControl.
    /// </summary>
    /// <param name="numTestedModule">Строка вида "chassis.module" для тестируемого модуля.</param>
    /// <param name="numVerificatModule">Строка вида "chassis.module" для проверяющего модуля.</param>
    /// <returns>True, если оба модуля найдены и инициализированы; иначе — false.</returns>
    private async Task<bool> SearchAndInitializeRelaySwitchModules(string numTestedModule, string numVerificatModule)
    {
      var searchService = new RelaySwitchModuleServices();
      var searckChassis = new ChassisManagerServices();

      // Разбираем "шасси.модуль" на две части
      var testedCoords = numTestedModule.Split('.').Select(int.Parse).ToArray();
      var verificatCoords = numVerificatModule.Split('.').Select(int.Parse).ToArray();

      var chassis = searckChassis.GetEntityById(testedCoords[0]);

      // 1) Получить список модулей из шасси тестируемого
      if (chassis == null)
      {
        await _userInteractionService
            .ShowMessageAsync(new ShowMessageModel(
                "Шасси тестируемого модуля не найдено!",
                ShowMessageModel.ErrorMessage.TitleColor));
        return false;
      }

      var list = searchService.GetDevicesByNumberChassis(testedCoords[0]);

      // 2) Найти сам модуль по его Id
      testedModuleRelayControl = list.FirstOrDefault(m => m.Number == testedCoords[1]);
      if (testedModuleRelayControl == null)
      {
        await _userInteractionService
            .ShowMessageAsync(new ShowMessageModel(
                "Тестируемый модуль не найден!",
                ShowMessageModel.ErrorMessage.TitleColor));
        return false;
      }

      // 3) То же самое для проверяющего модуля
      list = searchService.GetDevicesByNumberChassis(verificatCoords[0]);
      if (list == null || list.Count == 0)
      {
        await _userInteractionService
            .ShowMessageAsync(new ShowMessageModel(
                "Шасси проверяющего модуля не найдено!",
                ShowMessageModel.ErrorMessage.TitleColor));
        return false;
      }

      verificatModuleRelayControl = list
          .FirstOrDefault(m => m.Number == verificatCoords[1]);
      if (verificatModuleRelayControl == null)
      {
        await _userInteractionService
            .ShowMessageAsync(new ShowMessageModel(
                "Проверяющий модуль не найден!",
                ShowMessageModel.ErrorMessage.TitleColor));
        return false;
      }

      return true;
    }


    /// <summary>
    /// Выполняет основную логику теста: валидация, инициализация модулей,
    /// подготовка диапазона точек, выполнение трёх этапов перекрёстного теста.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task ExecuteTestProcess(IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      // 1. Валидация и парсинг трёх полей
      var (ok, message, tested, tester, range) = UIValidationHelperLightweight.TryValidateAndParseInput(_messageService, inputFieldProvider, inputHighlightService);
      if (!ok)
      {
        LogError($"Валидация не пройдена: {message}");
        return;
      }

      // 2. Присваивание ссылок на модули
      if (!await SearchAndInitializeRelaySwitchModules(tested, tester))
      {
        LogError("Не были присвоены ссылки на модули");
        return;
      }

      LogInformation("Запуск теста CrossTestMKR...");

      // Устанавливаем флаг сброса
      needReset = true;

      // 3. Преобразуем диапазон в список точек
      List<int> points = ParseRange(range);

      // 4. Подготовка оборудования
      await _userInteractionService.ShowMessageAsync(new ShowMessageModel("Инициализация оборудования"), IsBlockStart: true);
      await RelayModuleHelper.InitializeModule(_userInteractionService, testedModuleRelayControl, _userInteractionService, cancellationToken, "тестируемый");
      await RelayModuleHelper.InitializeModule(_userInteractionService, verificatModuleRelayControl, _userInteractionService, cancellationToken, "проверяющий");

      await _userInteractionService.ShowMessageAsync(new ShowMessageModel("Настройка оборудования"), IsBlockStart: true);
      await RelayModuleHelper.MeterEnableAsync(_userInteractionService, verificatModuleRelayControl, _userInteractionService, cancellationToken);

      // 5. Собственно сам тест
      await RunPart1(_userInteractionService, testedModuleRelayControl, verificatModuleRelayControl, points, SwitchingBus.A1, SwitchingBus.B1, BusPoint.A, BusPoint.B, cancellationToken);
      await RunPart2(_userInteractionService, testedModuleRelayControl, verificatModuleRelayControl, points, SwitchingBus.B1, SwitchingBus.A1, BusPoint.B, BusPoint.A, cancellationToken);
      await RunPart3(_userInteractionService, testedModuleRelayControl, verificatModuleRelayControl, cancellationToken, false);
    }

    /// <summary>
    /// Принудительно останавливает выполнение теста CrossTestMKR:
    ///  • выключает измеритель;
    ///  • сбрасывает оба модуля;
    ///  • выполняет общий Reset всей системы.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task Stop(CancellationToken cancellationToken)
    {
      if (!needReset) return;
      needReset = false;
    }

    #region Логика теста

    /// <summary>
    /// Выполняет первую часть перекрёстного теста:
    /// проверяет замыкания точек при подключении к A1,
    /// в диапазоне <paramref name="rangePoints"/>:
    ///  • проверяется наличие замыкания при подключении,
    ///  • затем — его отсутствие после отключения.
    /// </summary>
    /// <param name="tested_module">Тестируемый БК</param>
    /// <param name="verificat_module">Проверяющий БК</param>
    /// <param name="rangePoints">Список номеров точек для проверки.</param>
    /// <param name="switchingBus1">Шина, к которой подключается тестируемый БК</param>
    /// <param name="switchingBus2">Шина, к которой подключается проверяющий БК</param>
    /// <param name="bus1">Шина точка в тестируемом БК</param>
    /// <param name="bus2">Шина точка в проверяющем БК</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <param name="needRestartModuleAfter">
    /// Флаг сброса обоих БК по завершении:
    /// <c>true</c> — БК сбросятся по завершению,
    /// </param>
    /// <returns>True, если тест выполнен успешно</returns>
    private async Task<bool> RunPart1(
      IUserInteractionService messageService,
      IRelaySwitchModule tested_module,
      IRelaySwitchModule verificat_module,
      List<int> rangePoints,
      SwitchingBus switchingBus1,
      SwitchingBus switchingBus2,
      BusPoint bus1,
      BusPoint bus2,
      CancellationToken cancellationToken,
      bool needRestartModuleAfter = true)
    {
      await _userInteractionService.ShowMessageAsync(new ShowMessageModel($"ТЕСТ 1: проверка замыкания точек при подключении к A1"), IsBlockStart: true);
      bool result = await RunPointTest(messageService, tested_module, verificat_module, rangePoints, switchingBus1, switchingBus2, bus1, bus2, cancellationToken, needRestartModuleAfter);
      return result;
    }

    /// <summary>
    /// Выполняет первую часть перекрёстного теста:
    /// проверяет замыкания точек при подключении к B1,
    /// в диапазоне <paramref name="rangePoints"/>:
    ///  • проверяется наличие замыкания при подключении,
    ///  • затем — его отсутствие после отключения.
    /// </summary>
    /// <param name="tested_module">Тестируемый БК</param>
    /// <param name="verificat_module">Проверяющий БК</param>
    /// <param name="rangePoints">Список номеров точек для проверки.</param>
    /// <param name="switchingBus1">Шина, к которой подключается тестируемый БК</param>
    /// <param name="switchingBus2">Шина, к которой подключается проверяющий БК</param>
    /// <param name="bus1">Шина точка в тестируемом БК</param>
    /// <param name="bus2">Шина точка в проверяющем БК</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <param name="needRestartModuleAfter">
    /// Флаг сброса обоих БК по завершении:
    /// <c>true</c> — БК сбросятся по завершению,
    /// </param>
    /// <returns>True, если тест выполнен успешно</returns>
    private async Task<bool> RunPart2(IUserInteractionService messageService,
      IRelaySwitchModule tested_module,
      IRelaySwitchModule verificat_module,
      List<int> rangePoints,
      SwitchingBus switchingBus1,
      SwitchingBus switchingBus2,
      BusPoint bus1,
      BusPoint bus2,
      CancellationToken cancellationToken,
      bool needRestartModuleAfter = true)
    {
      await _userInteractionService.ShowMessageAsync(new ShowMessageModel($"\nТЕСТ 2: проверка замыкания точек при подключении к B1\n"), IsBlockStart: true);
      bool result = await RunPointTest(messageService, tested_module, verificat_module, rangePoints, switchingBus1, switchingBus2, bus1, bus2, cancellationToken, needRestartModuleAfter);
      return result;
    }

    /// <summary>
    /// Выполняет третью часть перекрёстного теста:
    /// проверка замыканий между всеми шинами.
    /// Для каждой пары шин проверяется корректность замыкания при подключении
    /// и его отсутствие при поочерёдном отключении.
    /// </summary>
    /// <param name="tested_module">Тестируемый БК</param>
    /// <param name="verificat_module">Проверяющий БК</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <param name="needRestartModuleAfter">
    /// Флаг сброса обоих БК по завершении:
    /// <c>true</c> — БК сбросятся по завершению,
    /// </param>
    /// <returns>True, если тест успешно завершён</returns>
    private async Task<bool> RunPart3(
    IUserInteractionService messageService,
    IRelaySwitchModule tested_module,
    IRelaySwitchModule verificat_module,
    CancellationToken cancellationToken,
    bool needRestartModuleAfter = true)
    {
      await _userInteractionService.ShowMessageAsync(new ShowMessageModel($"\nТЕСТ 3: проверка замыканий между всеми шинами\n"), IsBlockStart: true);

      // Подключаем МКР2 ко всем 8 шинам
      var allVerifBuses = new[] {
        SwitchingBus.A1, SwitchingBus.B1,
        SwitchingBus.A2, SwitchingBus.B2,
        SwitchingBus.A3, SwitchingBus.B3,
        SwitchingBus.A4, SwitchingBus.B4
    };
      foreach (var bus in allVerifBuses)
        await RelayModuleHelper.BusConnectAsync(bus, verificat_module, _userInteractionService, cancellationToken);

      // Подключаем первую точку МКР1 к шинам A и B
      await RelayModuleHelper.PointConnectAsync(tested_module, BusPoint.A, 1, _userInteractionService, cancellationToken);
      await RelayModuleHelper.PointConnectAsync(tested_module, BusPoint.B, 1, _userInteractionService, cancellationToken);

      // Циклическая часть для каждой пары шин A1/B1 … A4/B4
      var busPairs = new (SwitchingBus A, SwitchingBus B)[]
      {
        (SwitchingBus.A1, SwitchingBus.B1),
        (SwitchingBus.A2, SwitchingBus.B2),
        (SwitchingBus.A3, SwitchingBus.B3),
        (SwitchingBus.A4, SwitchingBus.B4)
      };

      foreach (var (busA, busB) in busPairs)
      {
        await RelayModuleHelper.BusConnectAsync(busA, tested_module, _userInteractionService, cancellationToken);
        await RelayModuleHelper.BusConnectAsync(busB, tested_module, _userInteractionService, cancellationToken);

        if (!await RelayModuleHelper.GetMeterAnswer(verificat_module, _userInteractionService, cancellationToken))
        {
          cancellationToken.ThrowIfCancellationRequested();
          await _userInteractionService.ShowMessageAsync(new ShowMessageModel("[ОБРЫВ ШИНЫ]", message: $"обрыв БК {tested_module.Number} от шин {busA} и {busB}", type: MessageType.Error));
        }
        else
        {
          cancellationToken.ThrowIfCancellationRequested();
          await _userInteractionService.ShowMessageAsync(new ShowMessageModel("[НОРМА]", message: $"замыкание шин {busA} и {busB}", type: MessageType.Success));
        }

        // Проверяем отсутствие обрыва на A
        await RelayModuleHelper.BusDisconnectAsync(busA, verificat_module, _userInteractionService, cancellationToken);

        if (await RelayModuleHelper.GetMeterAnswer(verificat_module, _userInteractionService, cancellationToken))
        {
          cancellationToken.ThrowIfCancellationRequested();
          await _userInteractionService.ShowMessageAsync(new ShowMessageModel("[ЗАМЫКАНИЕ ШИН]", message: $"замыкание при отключении БК {tested_module.Number} от шины {busA}", type: MessageType.Error));
        }
        else
        {
          cancellationToken.ThrowIfCancellationRequested();
          await _userInteractionService.ShowMessageAsync(new ShowMessageModel("[НОРМА]", message: $"замыкание на шине {busA} отсутствует", type: MessageType.Success));
        }

        // Проверяем отсутствие обрыва на B
        await RelayModuleHelper.BusConnectAsync(busA, verificat_module, _userInteractionService, cancellationToken);
        await RelayModuleHelper.BusDisconnectAsync(busB, verificat_module, _userInteractionService, cancellationToken);

        if (await RelayModuleHelper.GetMeterAnswer(verificat_module, _userInteractionService, cancellationToken))
        {
          cancellationToken.ThrowIfCancellationRequested();
          await _userInteractionService.ShowMessageAsync(new ShowMessageModel("[ЗАМЫКАНИЕ ШИН]", message: $"замыкание при отключении БК {tested_module.Number} от шины {busB}", type: MessageType.Error));
        }
        else
        {
          cancellationToken.ThrowIfCancellationRequested();
          await _userInteractionService.ShowMessageAsync(new ShowMessageModel("[НОРМА]", message: $"замыкание на шине {busB} отсутствует", type: MessageType.Error));
        }

        await RelayModuleHelper.BusConnectAsync(busB, verificat_module, _userInteractionService, cancellationToken);

        await RelayModuleHelper.BusDisconnectAsync(busA, tested_module, _userInteractionService, cancellationToken);
        await RelayModuleHelper.BusDisconnectAsync(busB, tested_module, _userInteractionService, cancellationToken);
      }

      if (needRestartModuleAfter)
      {
        await RelayModuleHelper.ResetModule(messageService, _userInteractionService, tested_module);
        await RelayModuleHelper.ResetModule(messageService, _userInteractionService, verificat_module);
      }
      return true;
    }


    #endregion

    #region Вспомогательные методы

    /// <summary>
    /// Преобразует строку диапазонов в уникальный список точек.
    /// Поддерживаются форматы: одиночные значения (например, "5"),
    /// и диапазоны (например, "2-4") через запятую.
    /// </summary>
    /// <param name="rangeText">Строка с диапазонами точек (например: "1, 2-5, 8").</param>
    /// <returns>Список уникальных номеров точек.</returns>
    private List<int> ParseRange(string rangeText)
    {
      // Используем HashSet для автоматического удаления дубликатов
      HashSet<int> pointsSet = new HashSet<int>();
      var segments = rangeText.Split(',');
      foreach (var segment in segments)
      {
        var trimmed = segment.Trim();
        if (trimmed.Contains('-'))
        {
          // формат "2-25"
          var bounds = trimmed.Split('-');
          if (bounds.Length == 2 &&
              int.TryParse(bounds[0].Trim(), out int start) &&
              int.TryParse(bounds[1].Trim(), out int end) &&
              start <= end)
          {
            for (int i = start; i <= end; i++)
              pointsSet.Add(i);
          }
        }
        else
        {
          // одиночное число
          if (int.TryParse(trimmed, out int singleVal))
            pointsSet.Add(singleVal);
        }
      }
      return pointsSet.ToList();
    }

    /// <summary>
    /// Выполняет тест подключения каждой точки из <paramref name="rangePoints"/> к шинам.
    /// Для каждой точки проверяется:
    ///  • наличие замыкания после подключения к <paramref name="bus1"/> и <paramref name="bus2"/>,
    ///  • отсутствие замыкания после отключения с одной из шин.
    /// </summary>
    /// <param name="tested_module">Тестируемый БК</param>
    /// <param name="verificat_module">Проверяющий БК</param>
    /// <param name="rangePoints">Список номеров точек для проверки.</param>
    /// <param name="switchingBus1">Шина, к которой подключается тестируемый БК</param>
    /// <param name="switchingBus2">Шина, к которой подключается проверяющий БК</param>
    /// <param name="bus1">Шина точка в тестируемом БК</param>
    /// <param name="bus2">Шина точка в проверяющем БК</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <param name="needRestartModuleAfter">
    /// Флаг сброса обоих БК по завершении:
    /// <c>true</c> — БК сбросятся по завершению,
    /// </param>
    /// <returns>True, если все проверки прошли успешно</returns>
    private async Task<bool> RunPointTest(
      IUserInteractionService messageService,
      IRelaySwitchModule tested_module,
      IRelaySwitchModule verificat_module,
      List<int> rangePoints,
      SwitchingBus switchingBus1,
      SwitchingBus switchingBus2,
      BusPoint bus1,
      BusPoint bus2,
      CancellationToken cancellationToken,
      bool needRestartModuleAfter = true)
    {
      // 1. Подключаем модули к заданным шинам
      await RelayModuleHelper.BusConnectAsync(switchingBus1, tested_module, _userInteractionService, cancellationToken);
      await RelayModuleHelper.BusConnectAsync(switchingBus2, tested_module, _userInteractionService, cancellationToken);
      await RelayModuleHelper.BusConnectAsync(switchingBus1, verificat_module, _userInteractionService, cancellationToken);
      await RelayModuleHelper.BusConnectAsync(switchingBus2, verificat_module, _userInteractionService, cancellationToken);

      // Подключаем все точки в МКР2 к выбранной шине
      // foreach (int point in rangePoints)
      //   await PointConnectAsync(verificat_module, bus2, point, cancellationToken);

      await _userInteractionService.ShowMessageAsync(new ShowMessageModel("Подлючение точек"));

      await verificat_module.PointManager.ConnectRelayGroupAsync(bus2, rangePoints.First(), rangePoints.Last(), _userInteractionService);

      foreach (int point in rangePoints)
      {
        await _userInteractionService.ShowMessageAsync(new ShowMessageModel($"Тест точки {point}"));
        var type = ShowMessageModel.MessageType.Success;

        await UserActionHelper.RunWithUserRepeatAsync(async () =>
        {
          cancellationToken.ThrowIfCancellationRequested();
          await RelayModuleHelper.PointConnectAsync(tested_module, bus1, point, _userInteractionService, cancellationToken);

          if (!await RelayModuleHelper.GetMeterAnswer(verificat_module, _userInteractionService, cancellationToken))
          {
            type = ShowMessageModel.MessageType.Error;
          }
          await _userInteractionService.ShowMessageAsync(new ShowMessageModel($"Результат проверки точки {point}", type: type) { IndentLevel = 2 }, skipPause: type == ShowMessageModel.MessageType.Error ? true : false);

          return type == ShowMessageModel.MessageType.Success ? true : false;
        }, _userInteractionService);


        type = ShowMessageModel.MessageType.Success;
        var message = string.Empty;

        await UserActionHelper.RunWithUserRepeatAsync(async () =>
        {
          await RelayModuleHelper.PointDisconnectAsync(verificat_module, bus2, point, _userInteractionService, cancellationToken);
          // Проверяем отсутствие замыкания между шинами
          cancellationToken.ThrowIfCancellationRequested();
          if (await RelayModuleHelper.GetMeterAnswer(verificat_module, _userInteractionService, cancellationToken))
          {
            type = ShowMessageModel.MessageType.Error;
            message = $"Замыкание при отключении точки {point} от шины {bus2}";
          }

          await _userInteractionService.ShowMessageAsync(new ShowMessageModel("Результат проверки", message: message, type: type) { IndentLevel = 2 }, skipPause: type == ShowMessageModel.MessageType.Error ? true : false);
          return type == ShowMessageModel.MessageType.Success ? true : false;
        }, _userInteractionService);


        // Подключаем точку МКР2 обратно и отключаем соответствующую точку МКР1 от своей шины
        await RelayModuleHelper.PointConnectAsync(verificat_module, bus2, point, _userInteractionService, cancellationToken);
        await RelayModuleHelper.PointDisconnectAsync(tested_module, bus1, point, _userInteractionService, cancellationToken);
      }

      if (needRestartModuleAfter)
      {
        await RelayModuleHelper.ResetModule(messageService, _userInteractionService, tested_module);
        await RelayModuleHelper.ResetModule(messageService, _userInteractionService, verificat_module);
      }

      return true;
    }

    #endregion
  }
}