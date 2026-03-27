using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.DataBase.Engine.Static.Devices;
using DataBaseConfiguration.Services.Device;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TestConsole.GPT
{
  internal class GPT_Test
  {
    internal static async Task RunAsync()
    {
      Console.WriteLine("=== Работа с GPT79904 ===");
      while (true)
      {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("1. Проверка подлючения");
        Console.WriteLine("2. Проверка времени нарастания");
        Console.WriteLine("3. Проверка скорости изменений параметров");
        Console.WriteLine("4. Тест завершения измерения");
        Console.WriteLine("5. Управление землей ACW/DCW");
        Console.WriteLine("0. Выход");

        Console.Write("Введите номер действия: ");
        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > 5)
        {
          Console.WriteLine("Неверный выбор. Попробуйте снова.");
          continue;
        }

        switch (choice)
        {
          case 1:
            await CheckConnection();
            break;

          case 2:
            await CheckTimeRamp();
            break;

          case 3:
            await CheckTime();
            break;

          case 4:
            await TestStop();
            break;

          case 5:
            await ConfigureGroundModeAsync();
            break;

          case 0:
            return;

          default:
            Console.WriteLine("Неверный выбор. Попробуйте снова.");
            break;
        }
      }
    }

    private static async Task ConfigureGroundModeAsync()
    {
      var device = SelectBreakdownTester();
      if (device == null)
      {
        return;
      }

      if (!await EnsureDeviceReadyAsync(device))
      {
        return;
      }

      Console.WriteLine("Выберите режим:");
      Console.WriteLine("1. ACW");
      Console.WriteLine("2. DCW");
      Console.WriteLine("0. Отмена");
      Console.Write("Введите номер режима: ");

      if (!int.TryParse(Console.ReadLine(), out int modeChoice) || modeChoice < 0 || modeChoice > 2)
      {
        Console.WriteLine("Неверный выбор режима.");
        return;
      }

      switch (modeChoice)
      {
        case 1:
          await ConfigureGroundModeForSelectedModeAsync(device.AcwManger.Mode, device.AcwManger.GroundMode, "ACW");
          break;

        case 2:
          await ConfigureGroundModeForSelectedModeAsync(device.DcwManger.Mode, device.DcwManger.GroundMode, "DCW");
          break;

        case 0:
          return;
      }
    }

    private static async Task ConfigureGroundModeForSelectedModeAsync(
      IModeConfigurable modeConfigurable,
      IGroundModeConfigurable groundModeConfigurable,
      string modeName)
    {
      try
      {
        var modeResult = await modeConfigurable.SetModeAsync();
        if (!modeResult.Success)
        {
          Console.WriteLine($"Не удалось установить режим {modeName}: {modeResult.Message}");
          return;
        }

        var currentState = await groundModeConfigurable.GetGroundModeAsync();
        Console.WriteLine($"Текущее состояние земли {modeName}: {(currentState ? "ON" : "OFF")}");
        Console.WriteLine("1. Включить землю");
        Console.WriteLine("2. Выключить землю");
        Console.WriteLine("3. Только показать текущее состояние");
        Console.WriteLine("0. Отмена");
        Console.Write("Введите номер действия: ");

        if (!int.TryParse(Console.ReadLine(), out int actionChoice) || actionChoice < 0 || actionChoice > 3)
        {
          Console.WriteLine("Неверный выбор действия.");
          return;
        }

        if (actionChoice == 0 || actionChoice == 3)
        {
          return;
        }

        var targetState = actionChoice == 1;
        var result = await groundModeConfigurable.SetGroundModeAsync(targetState);
        if (!result.Success)
        {
          Console.WriteLine($"Не удалось переключить землю {modeName}: {result.Message}");
          return;
        }

        var updatedState = await groundModeConfigurable.GetGroundModeAsync();
        Console.WriteLine($"Новое состояние земли {modeName}: {(updatedState ? "ON" : "OFF")}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка при работе с землей режима {modeName}: {ex.Message}");
      }
    }

    private static async Task<bool> EnsureDeviceReadyAsync(IBreakdownTester device)
    {
      try
      {
        var connectResult = await device.ConnectableManager.ConnectAsync();
        if (!connectResult.Connect)
        {
          Console.WriteLine($"Не удалось подключиться к {device.Name}: {connectResult.Answer}");
          return false;
        }

        return true;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка подключения к {device.Name}: {ex.Message}");
        return false;
      }
    }

    private static async Task CheckConnection()
    {
      var device = SelectBreakdownTester();
      if (device == null)
      {
        return;
      }

      for (int i = 0; i < 1000; i++)
      {
        await device.ConnectableManager.ConnectAsync();
        for (int j = 0; j < 1000; j++)
        {
          await device.ConnectableManager.InitializeAsync();
        }
        await device.ConnectableManager.DisconnectAsync();
      }
    }

    private static async Task CheckTimeRamp()
    {
      var device = SelectBreakdownTester();
      if (device != null)
      {
        await device.DcwManger.Mode.SetModeAsync();
        await device.DcwManger.Time.SetRampTimeAsync(0.1);
      }
    }

    private static async Task CheckTime()
    {
      var breakDown = SelectBreakdownTester();
      if (breakDown == null)
      {
        return;
      }

      Stopwatch stopwatch = new Stopwatch();
      stopwatch.Start();
      await breakDown.ConnectableManager.ConnectAsync();
      await breakDown.AcwManger.Mode.SetModeAsync();
      await breakDown.AcwManger.Time.SetTestTimeAsync(1);
      await breakDown.AcwManger.Time.SetRampTimeAsync(1);
      await breakDown.AcwManger.FrequencyConfigurable.SetFrequencyAsync(50);
      await breakDown.AcwManger.CurrentLimits.SetLowCurrentLimitAsync(0);
      await breakDown.AcwManger.CurrentLimits.SetHighCurrentLimitAsync(10);
      await breakDown.AcwManger.Voltage.SetVoltageAsync(100);

      stopwatch.Stop();

      Console.WriteLine($"Ticks: {stopwatch.ElapsedTicks}");
      Console.WriteLine($"Milliseconds: {stopwatch.ElapsedMilliseconds}");
      Console.WriteLine($"Seconds: {stopwatch.Elapsed.TotalSeconds:F3}");
    }

    private static IBreakdownTester SelectBreakdownTester()
    {
      var dbc = BreakdownTesters.GetAllAsync().GetAwaiter().GetResult();

      if (dbc == null || !dbc.Any())
      {
        Console.WriteLine("Нет доступных устройств.");
        return null;
      }

      Console.WriteLine("Выберите устройство для самоконтроля блокировочных реле:");

      int index = 1;
      foreach (var device in dbc)
      {
        Console.WriteLine($"{index}. {device.Name} (Номер шасси: {device.NumberChassis}, Номер устройства: {device.Number})");
        index++;
      }

      Console.Write("Введите номер устройства: ");
      if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= dbc.Count)
      {
        var selectedDevice = dbc.ElementAt(choice - 1);
        Console.WriteLine($"Выбрано устройство: {selectedDevice.Name} (ID: {selectedDevice.Id})");
        return selectedDevice;
      }

      Console.WriteLine("Некорректный выбор.");
      return null;
    }

    private static async Task TestStop()
    {
      var tester = BreakdownTesters.GetDevicesByNumberChassisAsync(1).GetAwaiter().GetResult().FirstOrDefault();
      if (tester != null)
      {
        await tester.IrManger.Measure.StopMeasure();
      }
    }
  }
}
