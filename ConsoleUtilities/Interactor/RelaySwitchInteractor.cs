using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleUtilities.Core;
using DataBaseConfiguration.Services;
using DataBaseConfiguration.Services.Device;
using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using static NewCore.Enum.DeviceEnum;

namespace ConsoleUtilities.Interactor
{
  public class RelaySwitchInteractor : IDeviceInteractor
  {
    public async Task RunAsync()
    {
      var devices = new RelaySwitchModuleServices().GetAll();

      if (devices == null || devices.Count == 0)
      {
        Console.WriteLine("Нет доступных модулей коммутации.");
        return;
      }

      Console.WriteLine("=== Модули коммутации реле ===");
      for (int i = 0; i < devices.Count; i++)
      {
        Console.WriteLine($"{i + 1}. {devices[i].Name} ({devices[i].Number})");
      }

      Console.Write("Выберите устройство: ");
      if (!int.TryParse(Console.ReadLine(), out int index) || index < 1 || index > devices.Count)
      {
        Console.WriteLine("Неверный выбор.");
        return;
      }

      var selected = devices[index - 1];
      Console.WriteLine($"Вы выбрали: {selected.Name}");

      await CommandLoop((IRelaySwitchModule)selected);
    }

    private async Task CommandLoop(IRelaySwitchModule module)
    {
      while (true)
      {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n=== Доступные действия ===");
        Console.WriteLine("1. Инициализация");
        Console.WriteLine("2. Работа с шинами (BusManager)");
        Console.WriteLine("3. Работа с измерителем (MeterManager)");
        Console.WriteLine("4. Работа с точками (PointManager)");
        Console.WriteLine("0. Назад");
        Console.ResetColor();

        Console.Write("Выберите действие: ");
        string input = Console.ReadLine();

        switch (input)
        {
          case "1":
            await InitializeModuleAsync(module);
            break;
          case "2":
            await HandleBusManagerAsync(module.BusManager);
            break;
          case "3":
            await HandleMeterManagerAsync(module.MeterManager);
            break;
          case "4":
            await HandlePointManagerAsync(module.PointManager);
            break;
          case "0":
            Console.WriteLine("Возврат в главное меню.");
            return;
          default:
            Console.WriteLine("Неверная команда. Попробуйте снова.");
            break;
        }
      }
    }

    private async Task InitializeModuleAsync(IRelaySwitchModule module)
    {
      var result = await module.ConnectableManager.InitializeAsync();
      if (result.Connect)
      {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[INIT] Инициализация шасси: {module.Name} [ОК]");
      }
      else
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[INIT] Инициализация шасси: {module.Name} [{result.Answer}]");
      }
      Console.ResetColor();
      return;
    }

    private async Task HandleBusManagerAsync(IBusManager busManager)
    {
      while (true)
      {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("\n=== Работа с шинами ===");
        Console.ResetColor();

        var buses = Enum.GetValues(typeof(SwitchingBus)).Cast<SwitchingBus>().ToList();

        for (int i = 0; i < buses.Count; i++)
        {
          Console.WriteLine($"{i + 1}. {buses[i]}");
        }

        Console.Write("Выберите шину (0 - назад): ");
        if (!int.TryParse(Console.ReadLine(), out int busChoice) || busChoice < 0 || busChoice > buses.Count)
        {
          Console.WriteLine("Неверный выбор.");
          continue;
        }

        if (busChoice == 0) return;

        var selectedBus = buses[busChoice - 1];

        Console.Write("Тип шины: [1] Низковольтная  [2] Высоковольтная: ");
        bool lowVoltage;
        string voltageInput = Console.ReadLine();
        if (voltageInput == "1") lowVoltage = true;
        else if (voltageInput == "2") lowVoltage = false;
        else
        {
          Console.WriteLine("Неверный ввод.");
          continue;
        }

        Console.WriteLine("Действие: [1] Подключить  [2] Отключить");
        string action = Console.ReadLine();

        if (action == "1")
        {
          var success = await busManager.ConnectBusAsync(selectedBus, lowVoltage);
          Console.ForegroundColor = success ? ConsoleColor.Green : ConsoleColor.Red;
          Console.WriteLine(success
            ? $"[ОК] Шина {selectedBus} подключена ({(lowVoltage ? "Низковольтная" : "Высоковольтная")})"
            : $"[ОШИБКА] Не удалось подключить шину {selectedBus}");
        }
        else if (action == "2")
        {
          var success = await busManager.DisconnectBusAsync(selectedBus, lowVoltage);
          Console.ForegroundColor = success ? ConsoleColor.Green : ConsoleColor.Red;
          Console.WriteLine(success
            ? $"[ОК] Шина {selectedBus} отключена ({(lowVoltage ? "Низковольтная" : "Высоковольтная")})"
            : $"[ОШИБКА] Не удалось отключить шину {selectedBus}");
        }
        else
        {
          Console.WriteLine("Неверное действие.");
        }

        Console.ResetColor();
        Console.WriteLine();
      }
    }

    private async Task HandleMeterManagerAsync(IMeterManager meterManager)
    {
      while (true)
      {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("\n=== Работа с измерителем ===");
        Console.WriteLine("1. Включить измеритель");
        Console.WriteLine("2. Выключить измеритель");
        Console.WriteLine("3. Получить ответ от измерителя");
        Console.WriteLine("0. Назад");
        Console.ResetColor();

        Console.Write("Выберите действие: ");
        string input = Console.ReadLine();

        switch (input)
        {
          case "1":
            {
              var success = await meterManager.ConnectMeterAsync();
              Console.ForegroundColor = success ? ConsoleColor.Green : ConsoleColor.Red;
              Console.WriteLine(success ? "[ОК] Измеритель включён" : "[ОШИБКА] Не удалось включить измеритель");
              break;
            }
          case "2":
            {
              var success = await meterManager.DisconnectMeterAsync();
              Console.ForegroundColor = success ? ConsoleColor.Green : ConsoleColor.Red;
              Console.WriteLine(success ? "[ОК] Измеритель отключён" : "[ОШИБКА] Не удалось отключить измеритель");
              break;
            }
          case "3":
            {
              var response = await meterManager.GetMeterResponseAsync();
              Console.ForegroundColor = response ? ConsoleColor.Green : ConsoleColor.Yellow;
              Console.WriteLine(response ? "[ДАННЫЕ] Замыкание подтверждено" : "[ДАННЫЕ] Замыкание не обнаружено");
              break;
            }
          case "0":
            return;

          default:
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("Неверная команда. Попробуйте снова.");
            break;
        }

        Console.ResetColor();
        Console.WriteLine();
      }
    }

    private async Task HandlePointManagerAsync(IPointManager pointManager)
    {
      while (true)
      {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("\n=== Работа с точками (реле) ===");
        Console.WriteLine("1. Подключить точку");
        Console.WriteLine("2. Отключить точку");
        Console.WriteLine("3. Подключить диапазон точек");
        Console.WriteLine("4. Отключить диапазон точек");
        Console.WriteLine("5. Проверить точку");
        Console.WriteLine("0. Назад");
        Console.ResetColor();

        Console.Write("Выберите действие: ");
        var input = Console.ReadLine();

        switch (input)
        {
          case "1":
            await HandleSinglePointAsync(pointManager, connect: true);
            break;
          case "2":
            await HandleSinglePointAsync(pointManager, connect: false);
            break;
          case "3":
            await HandleRangeAsync(pointManager, connect: true);
            break;
          case "4":
            await HandleRangeAsync(pointManager, connect: false);
            break;
          case "5":
            await HandlePointCheckAsync(pointManager);
            break;
          case "0":
            return;
          default:
            Console.WriteLine("Неверный ввод. Попробуйте снова.");
            break;
        }
      }
    }

    private async Task HandleSinglePointAsync(IPointManager manager, bool connect)
    {
      var bus = AskBusPoint();
      int point = AskInt("Введите номер точки: ");

      bool success = connect
        ? await manager.ConnectRelayAsync(bus, point)
        : await manager.DisconnectRelayAsync(bus, point);

      Console.ForegroundColor = success ? ConsoleColor.Green : ConsoleColor.Red;
      Console.WriteLine(success
        ? $"{(connect ? "Подключение" : "Отключение")} точки {point} выполнено успешно."
        : $"Не удалось {(connect ? "подключить" : "отключить")} точку {point}.");
      Console.ResetColor();
    }

    private async Task HandleRangeAsync(IPointManager manager, bool connect)
    {
      var bus = AskBusPoint();
      int first = AskInt("Введите первую точку диапазона: ");
      int last = AskInt("Введите последнюю точку диапазона: ");

      bool success = connect
        ? await manager.ConnectRelayGroupAsync(bus, first, last)
        : await manager.DisconnectRelayGroupAsync(bus, first, last);

      Console.ForegroundColor = success ? ConsoleColor.Green : ConsoleColor.Red;
      Console.WriteLine(success
        ? $"{(connect ? "Подключение" : "Отключение")} точек с {first} по {last} выполнено успешно."
        : $"Не удалось {(connect ? "подключить" : "отключить")} диапазон точек.");
      Console.ResetColor();
    }

    private async Task HandlePointCheckAsync(IPointManager manager)
    {
      int point = AskInt("Введите номер точки для проверки: ");
      string response = await manager.CheckPoint(point);
      Console.ForegroundColor = ConsoleColor.Cyan;
      Console.WriteLine($"Ответ на проверку точки {point}: {response}");
      Console.ResetColor();
    }

    private BusPoint AskBusPoint()
    {
      var values = Enum.GetValues(typeof(BusPoint)).Cast<BusPoint>().ToList();
      Console.WriteLine("Выберите шину:");
      for (int i = 0; i < values.Count; i++)
      {
        Console.WriteLine($"{i + 1}. {values[i]}");
      }

      while (true)
      {
        Console.Write("Введите номер шины: ");
        if (int.TryParse(Console.ReadLine(), out int index) &&
            index >= 1 && index <= values.Count)
        {
          return values[index - 1];
        }
        Console.WriteLine("Неверный ввод. Попробуйте снова.");
      }
    }
    private int AskInt(string message)
    {
      while (true)
      {
        Console.Write(message);
        if (int.TryParse(Console.ReadLine(), out int result))
          return result;

        Console.WriteLine("Неверное число. Попробуйте снова.");
      }
    }
  }
}
