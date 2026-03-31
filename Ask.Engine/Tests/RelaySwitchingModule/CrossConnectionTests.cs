using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Engine.Tests.Base;
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
        StopDelegate: async (CancellationToken token) =>
        {
          await Stop(token, userInteractionService);
        });
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
      var testedCoords = numTestedModule.Split('.').Select(int.Parse).ToArray();
      var verificatCoords = numVerificatModule.Split('.').Select(int.Parse).ToArray();
      var chassis = ChassisManagers.GetByIdAsync(testedCoords[0]).GetAwaiter().GetResult();

      if (chassis == null)
      {
        await _userInteractionService
            .ShowMessageAsync(new ShowMessageModel(
                "Шасси тестируемого модуля не найдено!",
                ShowMessageModel.ErrorMessage.TitleColor));
        return false;
      }

      var list = await RelaySwitchModules.GetDevicesByNumberChassisAsync(testedCoords[0]);

      testedModuleRelayControl = list.FirstOrDefault(m => m.Number == testedCoords[1]);
      if (testedModuleRelayControl == null)
      {
        await _userInteractionService
            .ShowMessageAsync(new ShowMessageModel(
                "Тестируемый модуль не найден!",
                ShowMessageModel.ErrorMessage.TitleColor));
        return false;
      }

      list = await RelaySwitchModules.GetDevicesByNumberChassisAsync(verificatCoords[0]);
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
      var (ok, message, tested, tester, range) = UIValidationHelperLightweight.TryValidateAndParseInput(_messageService, inputFieldProvider, inputHighlightService);
      if (!ok)
      {
        LogError($"Валидация не пройдена: {message}");
        return;
      }

      if (!await SearchAndInitializeRelaySwitchModules(tested, tester))
      {
        LogError("Не были присвоены ссылки на модули");
        return;
      }

      LogInformation("Запуск теста CrossTestMKR...");

      needReset = true;

      List<int> points = ParseRange(range);

      await _userInteractionService.ShowMessageAsync(new ShowMessageModel("Инициализация оборудования"), IsBlockStart: true);
      await RelayModuleHelper.InitializeModule(_userInteractionService, testedModuleRelayControl, _userInteractionService, cancellationToken, "тестируемый");
      await RelayModuleHelper.InitializeModule(_userInteractionService, verificatModuleRelayControl, _userInteractionService, cancellationToken, "проверяющий");

      await _userInteractionService.ShowMessageAsync(new ShowMessageModel("Настройка оборудования"), IsBlockStart: true);
      await RelayModuleHelper.MeterEnableAsync(_userInteractionService, verificatModuleRelayControl, _userInteractionService, cancellationToken);

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
    private async Task Stop(CancellationToken cancellationToken, IUserInteractionService _messageService = null)
    {
      await testedModuleRelayControl.ConnectableManager.ResetAsync(_messageService);
      await verificatModuleRelayControl.ConnectableManager.ResetAsync(_messageService);

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

      var allVerifBuses = new[] {
        SwitchingBus.A1, SwitchingBus.B1,
        SwitchingBus.A2, SwitchingBus.B2,
        SwitchingBus.A3, SwitchingBus.B3,
        SwitchingBus.A4, SwitchingBus.B4
    };
      foreach (var bus in allVerifBuses)
      {
        await RelayModuleHelper.BusConnectAsync(bus, verificat_module, _userInteractionService, cancellationToken);
      }

      await tested_module.PointManager.ConnectRelayAsync(BusPoint.A, 1, _userInteractionService);
      await tested_module.PointManager.ConnectRelayAsync(BusPoint.B, 1, _userInteractionService);

      await verificatModuleRelayControl.MeterManager.ConnectMeterAsync();

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

        try
        {
          if (await RelayModuleHelper.GetMeterAnswer(verificat_module, _userInteractionService, cancellationToken))
          {
            cancellationToken.ThrowIfCancellationRequested();
            await _userInteractionService.ShowMessageAsync(new ShowMessageModel("[НОРМА]", message: $"замыкание шин {busA} и {busB}", type: MessageType.Success) { IndentLevel = 2 });
          }
          else
          {
            cancellationToken.ThrowIfCancellationRequested();
            await _userInteractionService.ShowMessageAsync(new ShowMessageModel("[ОБРЫВ ШИНЫ]", message: $"обрыв БК {tested_module.Number} от шин {busA} и {busB}", type: MessageType.Error) { IndentLevel = 2 });
          }
        }
        catch
        {
          await _userInteractionService.ShowMessageAsync(new ShowMessageModel("[ОБРЫВ ШИНЫ]", message: $"обрыв БК {tested_module.Number} от шин {busA} и {busB}", type: MessageType.Error) { IndentLevel = 2 });
        }

        await RelayModuleHelper.BusDisconnectAsync(busA, tested_module, _userInteractionService, cancellationToken);

        try
        {
          if (!await RelayModuleHelper.GetMeterAnswer(verificat_module, _userInteractionService, cancellationToken))
          {
            cancellationToken.ThrowIfCancellationRequested();
            await _userInteractionService.ShowMessageAsync(new ShowMessageModel("[НОРМА]", message: $"замыкание на шине {busA} отсутствует", type: MessageType.Success) { IndentLevel = 2 });
          }
          else
          {
            throw new Exception();
          }
        }
        catch
        {
          await _userInteractionService.ShowMessageAsync(new ShowMessageModel("[ЗАМЫКАНИЕ ШИН]", message: $"замыкание при отключении БК {tested_module.Number} от шины {busA}", type: MessageType.Error) { IndentLevel = 2 });
        }

        await RelayModuleHelper.BusConnectAsync(busA, tested_module, _userInteractionService, cancellationToken);
        await RelayModuleHelper.BusDisconnectAsync(busB, tested_module, _userInteractionService, cancellationToken);

        try
        {
          if (await RelayModuleHelper.GetMeterAnswer(verificat_module, _userInteractionService, cancellationToken))
          {
            cancellationToken.ThrowIfCancellationRequested();
            await _userInteractionService.ShowMessageAsync(new ShowMessageModel("[ЗАМЫКАНИЕ ШИН]", message: $"замыкание при отключении БК {tested_module.Number} от шины {busB}", type: MessageType.Error) { IndentLevel = 2 });
          }
          else
          {
            cancellationToken.ThrowIfCancellationRequested();
            await _userInteractionService.ShowMessageAsync(new ShowMessageModel("[НОРМА]", message: $"замыкание на шине {busB} отсутствует", type: MessageType.Success) { IndentLevel = 2 });
          }
        }
        catch
        {
          cancellationToken.ThrowIfCancellationRequested();
          await _userInteractionService.ShowMessageAsync(new ShowMessageModel("[ЗАМЫКАНИЕ ШИН]", message: $"замыкание при отключении БК {tested_module.Number} от шины {busB}", type: MessageType.Error));
        }

        await RelayModuleHelper.BusDisconnectAsync(busA, tested_module, _userInteractionService, cancellationToken);
      }

      if (needRestartModuleAfter)
      {
        await tested_module.ConnectableManager.ResetAsync();
        await verificat_module.ConnectableManager.ResetAsync();
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
      HashSet<int> pointsSet = new HashSet<int>();
      var segments = rangeText.Split(',');
      foreach (var segment in segments)
      {
        var trimmed = segment.Trim();
        if (trimmed.Contains('-'))
        {
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
      await PreparePointTestAsync(tested_module, verificat_module, rangePoints, switchingBus1, switchingBus2, bus2, cancellationToken);
      await ExecutePointChecksAsync(tested_module, verificat_module, rangePoints, bus1, bus2, cancellationToken);
      await ResetModulesAfterPointTestAsync(messageService, tested_module, verificat_module, needRestartModuleAfter);

      return true;
    }

    /// <summary>
    /// Выполняет подготовку к тесту точек:
    /// подключает нужные шины на обоих БК и включает диапазон точек на проверяющем БК.
    /// </summary>
    /// <param name="tested_module">Тестируемый БК.</param>
    /// <param name="verificat_module">Проверяющий БК.</param>
    /// <param name="rangePoints">Диапазон точек для проверки.</param>
    /// <param name="switchingBus1">Первая шина подключения.</param>
    /// <param name="switchingBus2">Вторая шина подключения.</param>
    /// <param name="bus2">Шина точек на проверяющем БК.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task PreparePointTestAsync(
      IRelaySwitchModule tested_module,
      IRelaySwitchModule verificat_module,
      List<int> rangePoints,
      SwitchingBus switchingBus1,
      SwitchingBus switchingBus2,
      BusPoint bus2,
      CancellationToken cancellationToken)
    {
      await RelayModuleHelper.BusConnectAsync(switchingBus1, tested_module, _userInteractionService, cancellationToken);
      await RelayModuleHelper.BusConnectAsync(switchingBus2, tested_module, _userInteractionService, cancellationToken);
      await RelayModuleHelper.BusConnectAsync(switchingBus1, verificat_module, _userInteractionService, cancellationToken);
      await RelayModuleHelper.BusConnectAsync(switchingBus2, verificat_module, _userInteractionService, cancellationToken);

      await _userInteractionService.ShowMessageAsync(new ShowMessageModel("Подлючение точек"));
      await verificat_module.PointManager.ConnectRelayGroupAsync(bus2, rangePoints.First(), rangePoints.Last(), _userInteractionService);
    }

    /// <summary>
    /// Выполняет проверки для каждой точки из диапазона:
    /// контроль замыкания при подключении и контроль отсутствия замыкания после отключения.
    /// </summary>
    /// <param name="tested_module">Тестируемый БК.</param>
    /// <param name="verificat_module">Проверяющий БК.</param>
    /// <param name="rangePoints">Список проверяемых точек.</param>
    /// <param name="bus1">Шина точек тестируемого БК.</param>
    /// <param name="bus2">Шина точек проверяющего БК.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task ExecutePointChecksAsync(
      IRelaySwitchModule tested_module,
      IRelaySwitchModule verificat_module,
      List<int> rangePoints,
      BusPoint bus1,
      BusPoint bus2,
      CancellationToken cancellationToken)
    {
      foreach (int point in rangePoints)
      {
        await _userInteractionService.ShowMessageAsync(new ShowMessageModel($"Тест точки {point}"));
        await VerifyPointConnectionAsync(tested_module, verificat_module, bus1, point, cancellationToken);
        await VerifyPointDisconnectionAsync(verificat_module, bus2, point, cancellationToken);
        await RestorePointStateAsync(tested_module, verificat_module, bus1, bus2, point, cancellationToken);
      }
    }

    /// <summary>
    /// Проверяет наличие замыкания после подключения точки тестируемого БК.
    /// При ошибке дает пользователю повторить проверку.
    /// </summary>
    /// <param name="tested_module">Тестируемый БК.</param>
    /// <param name="verificat_module">Проверяющий БК.</param>
    /// <param name="busA">Шина точек тестируемого БК.</param>
    /// <param name="point">Номер проверяемой точки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task VerifyPointConnectionAsync(
      IRelaySwitchModule tested_module,
      IRelaySwitchModule verificat_module,
      BusPoint busA,
      int point,
      CancellationToken cancellationToken)
    {
      var type = ShowMessageModel.MessageType.Success;
      cancellationToken.ThrowIfCancellationRequested();

      await tested_module.PointManager.ConnectRelayAsync(busA, point, _userInteractionService);

      if (!await RelayModuleHelper.GetMeterAnswer(verificat_module, _userInteractionService, cancellationToken))
      {
        type = MessageType.Error;
      }

      await _userInteractionService.ShowMessageAsync(new ShowMessageModel($"Результат проверки точки {point}", type: type) { IndentLevel = 2 });
    }

    /// <summary>
    /// Проверяет отсутствие замыкания после отключения точки на проверяющем БК.
    /// При ошибке дает пользователю повторить проверку.
    /// </summary>
    /// <param name="verificat_module">Проверяющий БК.</param>
    /// <param name="busB">Шина точек проверяющего БК.</param>
    /// <param name="point">Номер проверяемой точки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task VerifyPointDisconnectionAsync(
      IRelaySwitchModule verificat_module,
      BusPoint busB,
      int point,
      CancellationToken cancellationToken)
    {
      var type = ShowMessageModel.MessageType.Success;
      cancellationToken.ThrowIfCancellationRequested();

      await verificat_module.PointManager.DisconnectRelayAsync(busB, point, _userInteractionService);

      if (await RelayModuleHelper.GetMeterAnswer(verificat_module, _userInteractionService, cancellationToken))
      {
        type = MessageType.Error;
      }

      string message = string.Empty;
      if (type == MessageType.Error)
      {
        message = $"Замыкание при отключении точки {point} от шины {busB}";
      }

      await _userInteractionService.ShowMessageAsync(new ShowMessageModel("Результат проверки", message: message, type: type) { IndentLevel = 2 });
    }

    /// <summary>
    /// Возвращает точку в исходное состояние после проверки:
    /// подключает точку обратно на проверяющем БК и отключает на тестируемом БК.
    /// </summary>
    /// <param name="tested_module">Тестируемый БК.</param>
    /// <param name="verificat_module">Проверяющий БК.</param>
    /// <param name="busA">Шина точек тестируемого БК.</param>
    /// <param name="busB">Шина точек проверяющего БК.</param>
    /// <param name="point">Номер проверяемой точки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task RestorePointStateAsync(
      IRelaySwitchModule tested_module,
      IRelaySwitchModule verificat_module,
      BusPoint busA,
      BusPoint busB,
      int point,
      CancellationToken cancellationToken)
    {
      await tested_module.PointManager.DisconnectRelayAsync(busA, point, _userInteractionService);
      await verificat_module.PointManager.ConnectRelayAsync(busB, point, _userInteractionService);
    }

    /// <summary>
    /// Выполняет сброс модулей после теста точек, если это требуется.
    /// </summary>
    /// <param name="messageService">Сервис сообщений для операций сброса.</param>
    /// <param name="tested_module">Тестируемый БК.</param>
    /// <param name="verificat_module">Проверяющий БК.</param>
    /// <param name="needRestartModuleAfter">
    /// Флаг сброса обоих БК по завершении:
    /// <c>true</c> — БК сбрасываются,
    /// <c>false</c> — состояние БК сохраняется.
    /// </param>
    private async Task ResetModulesAfterPointTestAsync(
      IUserInteractionService messageService,
      IRelaySwitchModule tested_module,
      IRelaySwitchModule verificat_module,
      bool needRestartModuleAfter)
    {
      if (!needRestartModuleAfter)
      {
        return;
      }

      await tested_module.ConnectableManager.ResetAsync(messageService);
      await verificat_module.ConnectableManager.ResetAsync(messageService);
    }

    #endregion
  }
}
