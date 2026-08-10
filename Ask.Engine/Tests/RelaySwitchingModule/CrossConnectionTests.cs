using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Engine.Tests.Base;
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

    private IExecutionController _controller;

    private IUserInteractionService _userInteractionService;

    /// <summary>
    /// Асинхронная настройка UI, добавление полей, запуск ProtocolSelfCheckControl.
    /// </summary>
    public async Task InitializeSettingsAsync(IExecutionController executionController, IUserInteractionService userInteractionService)
    {
      _controller = executionController;
      _userInteractionService = userInteractionService;
      ActionSettings settings = new ActionSettings()
      {
        StartDelegate = ExecuteTestProcess,
        CheckType = CheckType.Test,
      };

      _controller.SetSettings(settings);
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
        await ValidationMessages.PublishEquipmentLookupErrorAsync(
          "Шасси тестируемого модуля не найдено!",
          _userInteractionService);
        return false;
      }

      var list = await RelaySwitchModules.GetDevicesByNumberChassisAsync(testedCoords[0]);

      testedModuleRelayControl = list.FirstOrDefault(m => m.Number == testedCoords[1]);
      if (testedModuleRelayControl == null)
      {
        await ValidationMessages.PublishEquipmentLookupErrorAsync(
          "Тестируемый модуль не найден!",
          _userInteractionService);
        return false;
      }

      list = await RelaySwitchModules.GetDevicesByNumberChassisAsync(verificatCoords[0]);
      if (list == null || list.Count == 0)
      {
        await ValidationMessages.PublishEquipmentLookupErrorAsync(
          "Шасси проверяющего модуля не найдено!",
          _userInteractionService);
        return false;
      }

      verificatModuleRelayControl = list
          .FirstOrDefault(m => m.Number == verificatCoords[1]);
      if (verificatModuleRelayControl == null)
      {
        await ValidationMessages.PublishEquipmentLookupErrorAsync(
          "Проверяющий модуль не найден!",
          _userInteractionService);
        return false;
      }

      return true;
    }

    /// <summary>
    /// Выполняет основную логику теста: валидация, инициализация модулей,
    /// подготовка диапазона точек, выполнение трёх этапов перекрёстного теста.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task ExecuteTestProcess(ActionSettings settings, IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var (ok, message, tested, tester, range) = await UIValidationHelperLightweight.TryValidateAndParseInputAsync(
        _messageService,
        inputFieldProvider,
        inputHighlightService);
      if (!ok)
      {
        LogError($"Валидация не пройдена: {message}");
        return;
      }

      await UIValidationHelper.ShowTestInputAsync(
        _messageService,
        inputFieldProvider,
        new[]
        {
          ("Проверяемый модуль", tested),
          ("Проверяющий модуль", tester),
          ("Диапазон проверки", range)
        });

      if (!await SearchAndInitializeRelaySwitchModules(tested, tester))
      {
        LogError("Не были присвоены ссылки на модули");
        return;
      }

      LogInformation("Запуск теста CrossTestMKR...");

      List<int> points = ParseRange(range);

      await ExecutionMessages.PublishEquipmentInitializationAsync(_userInteractionService);
      await RelayModuleHelper.InitializeModule(_userInteractionService, testedModuleRelayControl, _userInteractionService, cancellationToken, "тестируемый");
      await RelayModuleHelper.InitializeModule(_userInteractionService, verificatModuleRelayControl, _userInteractionService, cancellationToken, "проверяющий");

      await ExecutionMessages.PublishEquipmentSetupAsync(_userInteractionService);
      await RelayModuleHelper.MeterEnableAsync(_userInteractionService, verificatModuleRelayControl, _userInteractionService, cancellationToken);

      await RunPart1(testedModuleRelayControl, verificatModuleRelayControl, points, SwitchingBus.A1, SwitchingBus.B1, BusPoint.A, BusPoint.B, cancellationToken);
      await RunPart2(testedModuleRelayControl, verificatModuleRelayControl, points, SwitchingBus.B1, SwitchingBus.A1, BusPoint.B, BusPoint.A, cancellationToken);
      await RunPart3(testedModuleRelayControl, verificatModuleRelayControl, cancellationToken, false);
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
      await ExecutionMessages.PublishTestStageAsync(
        "Этап 1. Проверка точек при подключении к шине A1",
        _userInteractionService);
      bool result = await RunPointTest(tested_module, verificat_module, rangePoints, switchingBus1, switchingBus2, bus1, bus2, cancellationToken, needRestartModuleAfter);
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
    private async Task<bool> RunPart2(
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
      await ExecutionMessages.PublishTestStageAsync(
        "Этап 2. Проверка точек при подключении к шине B1",
        _userInteractionService);
      bool result = await RunPointTest(tested_module, verificat_module, rangePoints, switchingBus1, switchingBus2, bus1, bus2, cancellationToken, needRestartModuleAfter);
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
      IRelaySwitchModule tested_module,
      IRelaySwitchModule verificat_module,
      CancellationToken cancellationToken,
      bool needRestartModuleAfter = true)
    {
      await ExecutionMessages.PublishTestStageAsync(
        "Этап 3. Проверка замыканий между шинами",
        _userInteractionService);

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

      await verificatModuleRelayControl.MeterManager.ConnectMeterAsync(_userInteractionService);

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

        await VerifyBusStateAsync(
          verificat_module,
          cancellationToken,
          expectedMeterAnswer: true,
          $"Проверка шин {busA} и {busB}",
          $"замыкание шин {busA} и {busB}",
          $"Проверка шин {busA} и {busB}",
          $"обрыв БК {tested_module.Number} от шин {busA} и {busB}");

        await RelayModuleHelper.BusDisconnectAsync(busA, tested_module, _userInteractionService, cancellationToken);

        await VerifyBusStateAsync(
          verificat_module,
          cancellationToken,
          expectedMeterAnswer: false,
          $"Проверка отключения шины {busA}",
          $"замыкание на шине {busA} отсутствует",
          $"Проверка отключения шины {busA}",
          $"замыкание при отключении БК {tested_module.Number} от шины {busA}");

        await RelayModuleHelper.BusConnectAsync(busA, tested_module, _userInteractionService, cancellationToken);
        await RelayModuleHelper.BusDisconnectAsync(busB, tested_module, _userInteractionService, cancellationToken);

        await VerifyBusStateAsync(
          verificat_module,
          cancellationToken,
          expectedMeterAnswer: false,
          $"Проверка отключения шины {busB}",
          $"замыкание на шине {busB} отсутствует",
          $"Проверка отключения шины {busB}",
          $"замыкание при отключении БК {tested_module.Number} от шины {busB}");

        await RelayModuleHelper.BusDisconnectAsync(busA, tested_module, _userInteractionService, cancellationToken);
      }

      if (needRestartModuleAfter)
      {
        await tested_module.ConnectableManager.ResetAsync();
        await verificat_module.ConnectableManager.ResetAsync();
      }
      return true;
    }

    private async Task VerifyBusStateAsync(
      IRelaySwitchModule verificat_module,
      CancellationToken cancellationToken,
      bool expectedMeterAnswer,
      string successHeader,
      string successMessage,
      string errorHeader,
      string errorMessage)
    {
      await UserActionHelper.RunWithUserRepeatAsync(async () =>
      {
        bool success;
        try
        {
          bool meterAnswer = await RelayModuleHelper.GetMeterAnswer(
            verificat_module,
            _userInteractionService,
            cancellationToken);
          success = meterAnswer == expectedMeterAnswer;
        }
        catch
        {
          cancellationToken.ThrowIfCancellationRequested();
          success = false;
        }

        await ExecutionMessages.PublishOperationResultAsync(
          success,
          successHeader,
          successMessage,
          errorHeader,
          errorMessage,
          _userInteractionService);

        return success;
      }, _userInteractionService);
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
      await ResetModulesAfterPointTestAsync(tested_module, verificat_module, needRestartModuleAfter);

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
      await RelayModuleHelper.MeterEnableAsync(
        _userInteractionService,
        verificat_module,
        _userInteractionService,
        cancellationToken);

      await RelayModuleHelper.BusConnectAsync(switchingBus1, tested_module, _userInteractionService, cancellationToken);
      await RelayModuleHelper.BusConnectAsync(switchingBus2, tested_module, _userInteractionService, cancellationToken);
      await RelayModuleHelper.BusConnectAsync(switchingBus1, verificat_module, _userInteractionService, cancellationToken);
      await RelayModuleHelper.BusConnectAsync(switchingBus2, verificat_module, _userInteractionService, cancellationToken);
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
        await ExecutionMessages.PublishTestPointAsync(point, _userInteractionService);
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
      cancellationToken.ThrowIfCancellationRequested();

      await tested_module.PointManager.ConnectRelayAsync(busA, point, _userInteractionService);

      await VerifyBusStateAsync(
        verificat_module,
        cancellationToken,
        expectedMeterAnswer: true,
        $"Проверка подключения точки {point}",
        "Замыкание обнаружено",
        $"Проверка подключения точки {point}",
        "Замыкание не обнаружено");
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
      cancellationToken.ThrowIfCancellationRequested();

      await verificat_module.PointManager.DisconnectRelayAsync(busB, point, _userInteractionService);

      await VerifyBusStateAsync(
        verificat_module,
        cancellationToken,
        expectedMeterAnswer: false,
        $"Проверка отключения точки {point}",
        "Замыкание отсутствует",
        $"Проверка отключения точки {point}",
        $"Замыкание сохраняется после отключения от шины [{busB}]");
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
      IRelaySwitchModule tested_module,
      IRelaySwitchModule verificat_module,
      bool needRestartModuleAfter)
    {
      if (!needRestartModuleAfter)
      {
        return;
      }

      await tested_module.ConnectableManager.ResetAsync(_userInteractionService);
      await verificat_module.ConnectableManager.ResetAsync(_userInteractionService);
    }

    #endregion
  }
}
