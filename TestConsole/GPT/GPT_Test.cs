using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using DataBaseConfiguration.Services.Device;
using System.Diagnostics;

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
        Console.WriteLine("0. Выход");

        Console.Write("Введите номер действия: ");
        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > 8)
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

          case 0:
            return;

          default:
            Console.WriteLine("Неверный выбор. Попробуйте снова.");
            break;
        }
      }
    }

    private static async Task CheckConnection()
    {
      var device = SelectBreakdownTester();
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
      var dbc = new BreakdownTesterServices().GetAll();

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
      else
      {
        Console.WriteLine("Некорректный выбор.");
      }

      return null;
    }

    private static async Task TestStop()
    {
      var tester = new BreakdownTesterServices().GetDevicesByNumberChassis(1).FirstOrDefault();
      if (tester != null)
      {
        await tester.IrManger.Measure.StopMeasure();
      }
    }
  }
}
