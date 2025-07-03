using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using Utilities.Models;
using UI.Controls.ProtocolNew;
using static NewCore.Enum.DeviceEnum;
using static Utilities.LoggerUtility;

namespace Mode.TestSuite.CrossTestMkr
{
  public partial class CrossTestMkrControl
  {
    #region Методы для общения

    /// <summary>
    /// Подключает заданный БК к указанной шине.
    /// </summary>
    /// <param name="bus">Шина</param>
    /// <param name="module">Блок коммутации</param>
    /// <param name="lowVoltage">
    /// Флаг режима низкого вольтажа:
    /// <c>true</c> — использовать низкий уровень напряжения,
    /// <c>false</c> — использовать стандартный (высокий) уровень напряжения.
    /// </param>
    private async Task<bool> BusConnectAsync(SwitchingBus bus, IRelaySwitchModule module, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return await module.BusManager.ConnectBusAsync(bus);
    }

    /// <summary>
    /// Отключает заданный БК от указанной шины.
    /// </summary>
    /// <param name="bus">Коммутационная шина</param>
    /// <param name="module">Блок коммутации</param>
    /// <param name="lowVoltage">
    /// Флаг режима низкого вольтажа:
    /// <c>true</c> — использовать низкий уровень напряжения,
    /// <c>false</c> — использовать стандартный (высокий) уровень напряжения.
    /// </param>
    private async Task<bool> BusDisconnectAsync(SwitchingBus bus, IRelaySwitchModule module, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return await module.BusManager.DisconnectBusAsync(bus);
    }

    /// <summary>
    /// Инициализирует БК и отображает сообщение об инициализации.
    /// </summary>
    /// <param name="module">Блок коммутации</param>
    /// <param name="roleName">Название роли блока коммутации</param>
    /// <returns>Возвращает <c>true</c>, если инициализация прошла успешно; иначе — <c>false</c>.</returns>
    private async Task<bool> InitializeModule(IRelaySwitchModule module, CancellationToken cancellationToken, string roleName = null)
    {
      var (state, answer) = await module.ConnectableManager.InitializeAsync();
      cancellationToken.ThrowIfCancellationRequested();
      LogInformation($"Ответ модуля - {answer}");
      return state;
    }

    /// <summary>
    /// Отключает БК и отображает сообщение об отключении.
    /// </summary>
    /// <param name="module">Блок коммутации</param>
    /// <returns>Возвращает <c>true</c>, если отключение прошло успешно; иначе — <c>false</c>.</returns>
    private async Task<bool> DisconnectModule(IRelaySwitchModule module)
    {
      var state = await module.ConnectableManager.DisconnectAsync();
      return state;
    }

    /// <summary>
    /// Выполняет сброс указанного БК.
    /// </summary>
    /// <param name="module">Блок коммутации</param>
    private async Task ResetModule(IRelaySwitchModule module)
    {
      await module.ConnectableManager.ResetAsync();
    }

    /// <summary>
    /// Подключает точку (реле) заданного БК к указанной шине.
    /// </summary>
    /// <param name="module">Блок коммутации</param>
    /// <param name="bus">Шина</param>
    /// <param name="point">Точка (реле)</param>
    /// <returns>Возвращает <c>true</c>, если точка успешно подключена; иначе — <c>false</c>.</returns>
    private async Task<bool> PointConnectAsync(IRelaySwitchModule module, BusPoint bus, int point, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return await module.PointManager.ConnectRelayAsync(bus, point);
    }

    /// <summary>
    /// Отключает точку (реле) заданного БК от указанной шины.
    /// </summary>
    /// <param name="module">Блок коммутации</param>
    /// <param name="bus">Шина</param>
    /// <param name="point">Точка (реле)</param>
    /// <returns>Возвращает <c>true</c>, если точка успешно отключена; иначе — <c>false</c>.</returns>
    private async Task<bool> PointDisconnectAsync(IRelaySwitchModule module, BusPoint bus, int point, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return await module.PointManager.DisconnectRelayAsync(bus, point);
    }

    /// <summary>
    /// Включает измеритель БК.
    /// </summary>
    /// <param name="module">Блок коммутации</param>
    /// <returns>Возвращает <c>true</c>, устройство включилось; иначе — <c>false</c>.</returns>
    private async Task<bool> MeterEnableAsync(IRelaySwitchModule module, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return await module.MeterManager.ConnectMeterAsync();
    }

    /// <summary>
    /// Отключает измеритель БК.
    /// </summary>
    /// <param name="module">Блок коммутации</param>
    /// <returns>Возвращает <c>true</c>, если устройство выключилось; иначе — <c>false</c>.</returns>
    private async Task<bool> MeterDisableAsync(IRelaySwitchModule module)
    {
      return await module.MeterManager.DisconnectMeterAsync();
    }

    /// <summary>
    /// Получает ответ измерителя указанного БК и отображает сообщение об измерении.
    /// </summary>
    /// <param name="module">Блок коммутации</param>
    /// <returns>Возвращает <c>true</c>, если есть замыкание; иначе — <c>false</c>.</returns>
    private async Task<bool> GetMeterAnswer(IRelaySwitchModule module, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return await module.MeterManager.GetMeterResponseAsync();
    }

    #endregion

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
      await ProtocolSelfCheckControl.ShowMessageAsync(
        new ShowMessageModel(
          $"ТЕСТ 1: проверка замыкания точек при подключении к A1",
          goodText.TitleColor
          ),
        IsBlockStart: true
        );
      bool result = await RunPointTest(tested_module, verificat_module, rangePoints, switchingBus1, switchingBus2, bus1, bus2, cancellationToken, needRestartModuleAfter);
      StepControlManager.ExitBlock();
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
      await ProtocolSelfCheckControl.ShowMessageAsync(
        new ShowMessageModel(
          $"\nТЕСТ 2: проверка замыкания точек при подключении к B1\n",
          goodText.TitleColor
          ),
        IsBlockStart: true
        );
      bool result = await RunPointTest(tested_module, verificat_module, rangePoints, switchingBus1, switchingBus2, bus1, bus2, cancellationToken, needRestartModuleAfter);
      StepControlManager.ExitBlock();
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
      await ProtocolSelfCheckControl.ShowMessageAsync(
        new ShowMessageModel(
          $"\nТЕСТ 3: проверка замыканий между всеми шинами\n",
          goodText.TitleColor
          ),
        IsBlockStart: true
        );

      // Подключаем МКР2 ко всем 8 шинам
      var allVerifBuses = new[] {
        SwitchingBus.A1, SwitchingBus.B1,
        SwitchingBus.A2, SwitchingBus.B2,
        SwitchingBus.A3, SwitchingBus.B3,
        SwitchingBus.A4, SwitchingBus.B4
    };
      foreach (var bus in allVerifBuses)
        await BusConnectAsync(bus, verificat_module, cancellationToken);

      // Подключаем первую точку МКР1 к шинам A и B
      await PointConnectAsync(tested_module, BusPoint.A, 1, cancellationToken);
      await PointConnectAsync(tested_module, BusPoint.B, 1, cancellationToken);

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
        await BusConnectAsync(busA, tested_module, cancellationToken);
        await BusConnectAsync(busB, tested_module, cancellationToken);

        if (!await GetMeterAnswer(verificat_module, cancellationToken))
        {
          cancellationToken.ThrowIfCancellationRequested();
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel(
              "[ОБРЫВ ШИНЫ]",
              errorText.Item2,
              $"обрыв БК {tested_module.Number} от шин {busA} и {busB}"
          ));
        }
        else
        {
          cancellationToken.ThrowIfCancellationRequested();
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel(
              "[НОРМА]",
              goodText.Item2,
              $"замыкание шин {busA} и {busB}"
          ));
        }

        // Проверяем отсутствие обрыва на A
        await BusDisconnectAsync(busA, verificat_module, cancellationToken);

        if (await GetMeterAnswer(verificat_module, cancellationToken))
        {
          cancellationToken.ThrowIfCancellationRequested();
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel(
              "[ЗАМЫКАНИЕ ШИН]",
              errorText.Item2,
              $"замыкание при отключении БК {tested_module.Number} от шины {busA}"
          ));
        }
        else
        {
          cancellationToken.ThrowIfCancellationRequested();
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel(
              "[НОРМА]",
              goodText.Item2,
              $"замыкание на шине {busA} отсутствует"
          ));
        }

        // Проверяем отсутствие обрыва на B
        await BusConnectAsync(busA, verificat_module, cancellationToken);
        await BusDisconnectAsync(busB, verificat_module, cancellationToken);

        if (await GetMeterAnswer(verificat_module, cancellationToken))
        {
          cancellationToken.ThrowIfCancellationRequested();
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel(
              "[ЗАМЫКАНИЕ ШИН]",
              errorText.Item2,
              $"замыкание при отключении БК {tested_module.Number} от шины {busB}"
          ));
        }
        else
        {
          cancellationToken.ThrowIfCancellationRequested();
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel(
              "[НОРМА]",
              goodText.Item2,
              $"замыкание на шине {busB} отсутствует"
          ));
        }

        await BusConnectAsync(busB, verificat_module, cancellationToken);

        await BusDisconnectAsync(busA, tested_module, cancellationToken);
        await BusDisconnectAsync(busB, tested_module, cancellationToken);
      }

      if (needRestartModuleAfter)
      {
        await ResetModule(tested_module);
        await ResetModule(verificat_module);
      }

      StepControlManager.ExitBlock();

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
      IRelaySwitchModule tested_module,
      IRelaySwitchModule verificat_module,
      List<int> rangePoints,
      SwitchingBus switchingBus1,
      SwitchingBus switchingBus2,
      BusPoint bus1,
      BusPoint bus2,
      CancellationToken cancellationToken,
      bool needRestartModuleAfter = true
      )
    {
      // 1. Подключаем модули к заданным шинам
      await BusConnectAsync(switchingBus1, tested_module, cancellationToken);
      await BusConnectAsync(switchingBus2, tested_module, cancellationToken);
      await BusConnectAsync(switchingBus1, verificat_module, cancellationToken);
      await BusConnectAsync(switchingBus2, verificat_module, cancellationToken);

      // Подключаем все точки в МКР2 к выбранной шине
      // foreach (int point in rangePoints)
      //   await PointConnectAsync(verificat_module, bus2, point, cancellationToken);

      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("Подлючение точек"));
      await verificat_module.PointManager.ConnectRelayGroupAsync(bus2, rangePoints.First(), rangePoints.Last());


      // Обработка точек
      foreach (int point in rangePoints)
      {
        await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"Тест точки {point}"));

        // Подключаем точку МКР1 к своей шине
        await PointConnectAsync(tested_module, bus1, point, cancellationToken);

        // Проверяем наличие замыкания между шинами
        var type = ShowMessageModel.MessageType.Success;

        if (!await GetMeterAnswer(verificat_module, cancellationToken))
        {
          type = ShowMessageModel.MessageType.Error;
        }
        await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"Результат проверки точки {point}", type: type) { IndentLevel = 2 });

        // Отключаем соответствующую точку МКР2 от своей шины
        await PointDisconnectAsync(verificat_module, bus2, point, cancellationToken);

        type = ShowMessageModel.MessageType.Success;
        var message = string.Empty;

        // Проверяем отсутствие замыкания между шинами
        cancellationToken.ThrowIfCancellationRequested();
        if (await GetMeterAnswer(verificat_module, cancellationToken))
        {
          type = ShowMessageModel.MessageType.Error;
          message = $"Замыкание при отключении точки {point} от шины {bus2}";
        }

        await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("Результат проверки", message: message, type: type) { IndentLevel = 2 });

        // Подключаем точку МКР2 обратно и отключаем соответствующую точку МКР1 от своей шины
        await PointConnectAsync(verificat_module, bus2, point, cancellationToken);
        await PointDisconnectAsync(tested_module, bus1, point, cancellationToken);
      }

      if (needRestartModuleAfter)
      {
        await ResetModule(tested_module);
        await ResetModule(verificat_module);
      }

      return true;
    }

    #endregion
  }
}