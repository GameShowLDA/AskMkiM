using Utilities.Models;
using static Utilities.LoggerUtility;

namespace Mode.TestSuite.CrossTestMkr
{
  public partial class CrossTestMkrControl
  {
    #region Методы для общения

    private async Task<bool> BusConnectAsync(string bus, string module, CancellationToken cancellationToken = default, bool voltage = true)
    {
      //LogInformation($"Шина {bus} подключена в {module}");
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"Шина {bus} подключена в {module}"));
      return true;
    }
    private async Task<bool> BusDisconnectAsync(string bus, string module, bool voltage = true, CancellationToken cancellationToken = default)
    {
      //LogInformation($"Шина {bus} отключена в {module}");
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"Шина {bus} отключена в {module}"));
      return true;
    }
    private async Task<bool> InitializeModule(string module, CancellationToken cancellationToken = default)
    {
      //LogInformation($"Модуль {module} инициализирован");
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"Модуль типа \"МКР350\" инициализирован - {module}"));
      return true;
    }
    private async Task ResetModule(string module, CancellationToken cancellationToken = default)
    {
      //LogInformation($"Сброс модуля {module}");
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"Модуль типа \"МКР350\" сброшен - {module}"));
    }
    private async Task<bool> PointConnectAsync(string module ,string bus, string point, CancellationToken cancellationToken = default)
    {
      //LogInformation($"Реле {point} подключена к шине {bus} в модуле {module}");
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"Точка {point} подключена к шине {bus} в модуле {module}"));
      return true;
    }
    private async Task<bool> PointDisconnectAsync(string module, string bus, string point, CancellationToken cancellationToken = default)
    {
      //LogInformation($"Реле {point} отключена от шины {bus} в модуле {module}");
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"Точка {point} отключена от шины {bus} в модуле {module}"));
      return true;
    }
    private async Task<bool> MeterEnableAsync(string module, CancellationToken cancellationToken = default)
    {
      //LogInformation($"Вкл. измеритель в модуле {module}");
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"Включен измеритель в модуле {module}"));
      return true;
    }
    private async Task<bool> MeterDisableAsync(string module, CancellationToken cancellationToken = default)
    {
      //LogInformation($"Выкл. измеритель в модуле {module}");
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"Выключен измеритель в модуле {module}"));
      return true;
    }
    private async Task<bool> GetMeterAnswer(string module, CancellationToken cancellationToken = default)
    {
      return true;
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
    private async Task<bool> RunPart1(string tested_module, string verificat_module, List<int> rangePoints, CancellationToken cancellationToken = default)
    {
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\nЧАСТЬ 1\n"));
      return await RunPointTest(tested_module, verificat_module, rangePoints, "A1", "B1");
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
    private async Task<bool> RunPart2(string tested_module, string verificat_module, List<int> rangePoints, CancellationToken cancellationToken = default)
    {
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\nЧАСТЬ 2\n"));
      return await RunPointTest(tested_module, verificat_module, rangePoints, "B1", "A1");
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
    private async Task<bool> RunPart3(string tested_module, string verificat_module, List<int> rangePoints, CancellationToken cancellationToken = default)
    {
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\nЧАСТЬ 3\n"));
      // Подключаем МКР2 ко всем 8 шинам
      for (int i = 1; i <= 4; i++)
      {
        await BusConnectAsync($"A{i}", verificat_module);
        await BusConnectAsync($"B{i}", verificat_module);
      }

      // Для каждой пары шин (A{i}, B{i})
      for (int i = 1; i <= 4; i++)
      {
        // Шаг 12: Подключаем МКР1 к шинам A{i} и B{i}
        await BusConnectAsync($"A{i}", tested_module);
        await BusConnectAsync($"B{i}", tested_module);

        // Проверяем наличие замыкания между шинами A{i} и B{i}
        if (!await GetMeterAnswer(verificat_module))
        {
          string errorMsg = $"обрыв МКР1 от шин A{i}, B{i}";
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel(errorMsg));
          await ResetModule(tested_module);
          await ResetModule(verificat_module);
          return false;
        }

        // Шаг 13: Отключаем МКР2 от шины A{i} и проверяем отсутствие замыкания
        await BusDisconnectAsync($"A{i}", verificat_module);
        if (await GetMeterAnswer(verificat_module))
        {
          string errorMsg = $"замыкание при отключении МКР1 от шины A{i}";
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel(errorMsg));
          await ResetModule(tested_module);
          await ResetModule(verificat_module);
          return false;
        }

        // Шаг 14: Повторно подключаем МКР2 к A{i} и отключаем от B{i}, затем проверяем отсутствие замыкания
        await BusConnectAsync($"A{i}", verificat_module);
        await BusDisconnectAsync($"B{i}", verificat_module);
        if (await GetMeterAnswer(verificat_module))
        {
          string errorMsg = $"замыкание при отключении МКР1 от шины B{i}";
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel(errorMsg));
          await ResetModule(tested_module);
          await ResetModule(verificat_module);
          return false;
        }

        // Восстанавливаем подключение МКР2 к шине B{i} для корректного перехода к следующей паре
        await BusConnectAsync($"B{i}", verificat_module);
      }

      await ResetModule(tested_module);
      await ResetModule(verificat_module);

      return true;
    }

    #endregion


    #region Вспомогательные методы

    private List<int> ParseRange(string rangeText)
    {
      var result = new List<int>();
      var segments = rangeText.Split(',');
      foreach (var segment in segments)
      {
        var trimmed = segment.Trim();
        if (trimmed.Contains('-'))
        {
          // формат "2-25"
          var bounds = trimmed.Split('-');
          if (bounds.Length == 2
              && int.TryParse(bounds[0].Trim(), out int start)
              && int.TryParse(bounds[1].Trim(), out int end)
              && start <= end)
          {
            for (int i = start; i <= end; i++)
              result.Add(i);
          }
        }
        else
        {
          // одиночное число
          if (int.TryParse(trimmed, out int singleVal))
            result.Add(singleVal);
        }
      }
      return result;
    }

    private async Task<bool> RunPointTest(
    string tested_module,
    string verificat_module,
    List<int> rangePoints,
    string bus1,
    string bus2,
    CancellationToken cancellationToken = default
)
    {
      // Проверка на отмену перед началом выполнения
      cancellationToken.ThrowIfCancellationRequested();

      await BusConnectAsync(bus1, tested_module, cancellationToken);
      cancellationToken.ThrowIfCancellationRequested();
      await BusConnectAsync(bus2, tested_module, cancellationToken);
      cancellationToken.ThrowIfCancellationRequested();
      await BusConnectAsync(bus1, verificat_module, cancellationToken);
      cancellationToken.ThrowIfCancellationRequested();
      await BusConnectAsync(bus2, verificat_module, cancellationToken);
      cancellationToken.ThrowIfCancellationRequested();

      foreach (int point in rangePoints)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await PointConnectAsync(verificat_module, bus2, point.ToString(), cancellationToken);
      }

      foreach (int point in rangePoints)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await PointConnectAsync(tested_module, bus1, point.ToString(), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        bool closed = await GetMeterAnswer(verificat_module, cancellationToken);
        if (!closed)
        {
          string errorMsg = $"обрыв точки {point} от шины {tested_module}";
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel(errorMsg));
          await ResetModule(tested_module, cancellationToken);
          await ResetModule(verificat_module, cancellationToken);
          return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await PointDisconnectAsync(verificat_module, bus2, point.ToString(), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (await GetMeterAnswer(verificat_module, cancellationToken))
        {
          string errorMsg = $"замыкание при отключении точки {point} от шины {tested_module}";
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel(errorMsg));
          await ResetModule(tested_module, cancellationToken);
          await ResetModule(verificat_module, cancellationToken);
          return false;
        }
      }

      cancellationToken.ThrowIfCancellationRequested();
      await ResetModule(tested_module, cancellationToken);
      cancellationToken.ThrowIfCancellationRequested();
      await ResetModule(verificat_module, cancellationToken);

      return true;
    }



    #endregion
  }
}