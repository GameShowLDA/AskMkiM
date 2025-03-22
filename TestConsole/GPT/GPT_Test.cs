using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfig.DataBase.Services;
using NewCore.Base.Interface.Main;

namespace TestConsole.GPT
{
  internal class GPT_Test
  {
    internal static async Task RunAsync()
    {
      Console.WriteLine("=== Самоконтроль МИНТ ===");
      while (true)
      {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("1. Проверка подлючения");
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
      if (device != null)
        await device.IrManger.SetModeAsync();
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
  }
}
