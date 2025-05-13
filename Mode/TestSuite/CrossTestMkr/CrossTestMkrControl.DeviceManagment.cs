using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using Utilities.Models;
using static NewCore.Enum.DeviceEnum;
using static Utilities.LoggerUtility;

namespace Mode.TestSuite.CrossTestMkr
{
  public partial class CrossTestMkrControl
  {
    #region Методы для общения

    private async Task<bool> BusConnectAsync(SwitchingBus bus, IRelaySwitchModule module, bool lowVoltage = true)
    {
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"Шина {bus} подключена в БК {module.Number}"));
      return await module.BusManager.ConnectBusAsync(bus, lowVoltage);
    }
    private async Task<bool> BusDisconnectAsync(SwitchingBus bus, IRelaySwitchModule module, bool lowVoltage = true)
    {
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"Шина {bus} подключена в БК {module.Number}"));
      return await module.BusManager.DisconnectBusAsync(bus, lowVoltage);
    }
    private async Task<bool> InitializeModule(IRelaySwitchModule module)
    {
      var (state, answer) =await module.StateManager.Initialize();
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"Модуль БК {module.Number} инициализирован"));
      LogInformation($"Ответ модуля - {answer}");
      return state;
    }
    private async Task ResetModule(IRelaySwitchModule module)
    {
      await module.StateManager.ResetAsync();
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"Модуль БК {module.Number} сброшен"));
    }
    private async Task<bool> PointConnectAsync(IRelaySwitchModule module, BusPoint bus, int point)
    {
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"Точка {point} подключена к шине {bus} в модуле БК {module.Number}"));
      return await module.PointManager.ConnectRelayAsync(bus, point);
    }
    private async Task<bool> PointDisconnectAsync(IRelaySwitchModule module, BusPoint bus, int point)
    {
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"Точка {point} отключена от шины {bus} в модуле БК {module.Number}"));
      return await module.PointManager.DisconnectRelayAsync(bus, point);
    }
    private async Task<bool> MeterEnableAsync(IRelaySwitchModule module)
    {
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"Включен измеритель в модуле БК {module.Number}"));
      return await module.MeterManager.ConnectMeterAsync();
    }
    private async Task<bool> MeterDisableAsync(IRelaySwitchModule module)
    {
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"Выключен измеритель в модуле БК {module.Number}"));
      return await module.MeterManager.DisconnectMeterAsync();
    }
    private async Task<bool> GetMeterAnswer(IRelaySwitchModule module)
    {
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"Проведение измерения БК {module.Number}"));
      return await module.MeterManager.GetMeterResponseAsync();
    }

    #endregion

    #region Логика теста

    /// <summary>
    /// ЧАСТЬ 1
    /// 1. МКР1 подключается к шине A1, МКР2 – к шине B1, точки заданного диапазона в МКР2 подключаются к шине B1.
    /// 2. Для каждой точки: 
    ///    - Подключается соответствующая точка в МКР1 к шине A1.
    ///    - Проверяется наличие замыкания между шинами A1 и B1. Если замыкания нет, выдаётся сообщение об обрыве и тест прерывается.
    ///    - Точка в МКР2 отключается от шины B1.
    ///    - Проверяется отсутствие замыкания между шинами A1 и B1. Если замыкание обнаружено – выдаётся сообщение и тест прерывается.
    /// </summary>
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
      //await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\nЧАСТЬ 1\n"));
      return await RunPointTest(tested_module, verificat_module, rangePoints, switchingBus1, switchingBus2, bus1, bus2, cancellationToken, needRestartModuleAfter);
    }

    /// <summary>
    /// ЧАСТЬ 2
    /// 6. Точки заданного диапазона в МКР2 сбрасываются с шины B1 и подключаются к шине A1.
    /// 7. Для каждой точки:
    ///    - Точка в МКР1 подключается к шине B1.
    ///    - Проверяется наличие замыкания между шинами A1 и B1. Если замыкания нет – сообщение об обрыве точки и останов теста.
    ///    - Точка в МКР2 отключается от шины A1.
    ///    - Проверяется отсутствие замыкания между шинами A1 и B1. При обнаружении замыкания – сообщение и останов теста.
    /// </summary>
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
      //await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\nЧАСТЬ 2\n"));
      return await RunPointTest(tested_module, verificat_module, rangePoints, switchingBus1, switchingBus2, bus1, bus2, cancellationToken, needRestartModuleAfter);
    }

    /// <summary>
    /// ЧАСТЬ 3
    /// 11. МКР2 подключается к 8 шинам: A1, A2, A3, A4, B1, B2, B3, B4.
    /// 12. Для каждой пары шин (A{i}, B{i}), i = 1..4:
    ///    - МКР1 подключается к шинам A{i} и B{i} и проверяется наличие замыкания между ними.
    ///      Если замыкания нет – сообщение "обрыв МКР1 от шин A{i}, B{i}" и тест останавливается.
    /// 13. МКР2 отключается от шины A{i} и проверяется отсутствие замыкания между A{i} и B{i}.
    ///      При наличии замыкания – сообщение "замыкание при отключении МКР1 от шины A{i}" и тест останавливается.
    /// 14. МКР2 повторно подключается к шине A{i} и отключается от шины B{i},
    ///      после чего проверяется отсутствие замыкания между A{i} и B{i}.
    ///      При наличии замыкания – сообщение "замыкание при отключении МКР1 от шины B{i}" и тест останавливается.
    /// 15. Аналогичные проверки выполняются для всех пар шин.
    /// </summary>
    private async Task<bool> RunPart3(
    IRelaySwitchModule tested_module,
    IRelaySwitchModule verificat_module,
    CancellationToken cancellationToken,
    bool needRestartModuleAfter = true)
    {
      // Подключаем МКР2 ко всем 8 шинам
      var allVerifBuses = new[] {
        SwitchingBus.A1, SwitchingBus.B1,
        SwitchingBus.A2, SwitchingBus.B2,
        SwitchingBus.A3, SwitchingBus.B3,
        SwitchingBus.A4, SwitchingBus.B4
    };
      foreach (var bus in allVerifBuses)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await BusConnectAsync(bus, verificat_module);
      }

      // Подключаем первую точку МКР1 к шинам A и B
      cancellationToken.ThrowIfCancellationRequested();
      await PointConnectAsync(tested_module, BusPoint.A, 1);
      await PointConnectAsync(tested_module, BusPoint.B, 1);

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
        cancellationToken.ThrowIfCancellationRequested();
        await BusConnectAsync(busA, tested_module);
        await BusConnectAsync(busB, tested_module);

        cancellationToken.ThrowIfCancellationRequested();
        if (!await GetMeterAnswer(verificat_module))
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel(
              "[ОБРЫВ ШИНЫ]",
              errorText.Item2,
              $"обрыв БК {tested_module.Number} от шин {busA} и {busB}"
          ));
        else
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel(
              "[НОРМА]",
              goodText.Item2,
              $"замыкание шин {busA} и {busB}"
          ));

        // Проверяем отсутствие обрыва на A
        cancellationToken.ThrowIfCancellationRequested();
        await BusDisconnectAsync(busA, verificat_module);

        cancellationToken.ThrowIfCancellationRequested();
        if (await GetMeterAnswer(verificat_module))
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel(
              "[ЗАМЫКАНИЕ ШИН]",
              errorText.Item2,
              $"замыкание при отключении БК {tested_module.Number} от шины {busA}"
          ));
        else
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel(
              "[НОРМА]",
              goodText.Item2,
              $"замыкание на шине {busA} отсутствует"
          ));

        // Проверяем отсутствие обрыва на B
        cancellationToken.ThrowIfCancellationRequested();
        await BusConnectAsync(busA, verificat_module);
        await BusDisconnectAsync(busB, verificat_module);

        cancellationToken.ThrowIfCancellationRequested();
        if (await GetMeterAnswer(verificat_module))
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel(
              "[ЗАМЫКАНИЕ ШИН]",
              errorText.Item2,
              $"замыкание при отключении БК {tested_module.Number} от шины {busB}"
          ));
        else
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel(
              "[НОРМА]",
              goodText.Item2,
              $"замыкание на шине {busB} отсутствует"
          ));

        cancellationToken.ThrowIfCancellationRequested();
        await BusConnectAsync(busB, verificat_module);

        cancellationToken.ThrowIfCancellationRequested();
        await BusDisconnectAsync(busA, tested_module);
        await BusDisconnectAsync(busB, tested_module);
      }

      if (needRestartModuleAfter)
      {
        await ResetModule(tested_module);
        await ResetModule(verificat_module);
      }

      return true;
    }


    #endregion

    #region Вспомогательные методы

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
      // Проверка на отмену перед началом выполнения
      cancellationToken.ThrowIfCancellationRequested();

      // 1. Подключаем модули к заданным шинам
      await BusConnectAsync(switchingBus1, tested_module);
      cancellationToken.ThrowIfCancellationRequested();
      await BusConnectAsync(switchingBus2, tested_module);
      cancellationToken.ThrowIfCancellationRequested();
      await BusConnectAsync(switchingBus1, verificat_module);
      cancellationToken.ThrowIfCancellationRequested();
      await BusConnectAsync(switchingBus2, verificat_module);
      cancellationToken.ThrowIfCancellationRequested();

      // Подключаем все точки в МКР2 к выбранной шине
      foreach (int point in rangePoints)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await PointConnectAsync(verificat_module, bus2, point);
      }

      // Обработка точек
      foreach (int point in rangePoints)
      {
        // Подключаем точку МКР1 к своей шине
        cancellationToken.ThrowIfCancellationRequested();
        await PointConnectAsync(tested_module, bus1, point);

        // Проверяем наличие замыкания между шинами
        if (!await GetMeterAnswer(verificat_module))
        {
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("[НЕТ ПОДКЛЮЧЕНИЯ]", errorText.Item2, $"обрыв точки {point} от шины {bus1} в БК {tested_module.Number}"));
        }
        else
        {
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("[НОРМА]", goodText.Item2, $"точка {point} замкнута"));
        }

        // Отключаем соответствующую точку МКР2 от своей шины
        cancellationToken.ThrowIfCancellationRequested();
        await PointDisconnectAsync(verificat_module, bus2, point);

        // Проверяем отсутствие замыкания между шинами
        if (await GetMeterAnswer(verificat_module))
        {
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("[ЛИШНЕЕ ПОДКЛЮЧЕНИЕ]", errorText.Item2, $"замыкание при отключении точки {point} от шины {bus2} в БК {verificat_module.Number}"));
        }
        else 
        {
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("[НОРМА]", goodText.Item2, $"замыкание точки {point} отсутствует"));
        }

        // Отключаем соответствующую точку МКР1 от своей шины
        cancellationToken.ThrowIfCancellationRequested();
        await PointDisconnectAsync(tested_module, bus1, point);
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