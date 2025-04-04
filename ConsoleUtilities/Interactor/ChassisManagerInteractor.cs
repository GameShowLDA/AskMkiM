using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleUtilities.Core;
using DataBaseConfiguration.Services;
using NewCore.Base.Interface.Main;

namespace ConsoleUtilities.Interactor
{
  public class ChassisManagerInteractor : IDeviceInteractor
  {
    public async Task RunAsync()
    {
      var devices = new ChassisManagerServices().GetAll();

      if (devices == null || devices.Count == 0)
      {
        Console.WriteLine("Нет доступных шасси.");
        return;
      }

      Console.WriteLine("=== Менеджеры шасси ===");
      for (int i = 0; i < devices.Count; i++)
      {
        Console.WriteLine($"{i + 1}. {devices[i].Name}({devices[i].Number})");
      }

      Console.Write("Выберите устройство: ");
      if (!int.TryParse(Console.ReadLine(), out int index) || index < 1 || index > devices.Count)
      {
        Console.WriteLine("Неверный выбор.");
        return;
      }

      var selected = devices[index - 1];
      Console.WriteLine($"Вы выбрали: {selected.Name}");

      await CommandLoop(selected);
    }

    private async Task CommandLoop(IChassisManager chassis)
    {
      while (true)
      {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n=== Доступные действия ===");
        Console.WriteLine("1. Инициализация");
        Console.WriteLine("2. Включить питание");
        Console.WriteLine("3. Выключить питание");
        Console.WriteLine("0. Назад");
        Console.ResetColor();

        Console.Write("Выберите действие: ");
        string input = Console.ReadLine();

        switch (input)
        {
          case "1":
            await InitializeChassisAsync(chassis);
            break;
          case "2":
            await PowerOnChassisAsync(chassis);
            break;
          case "3":
            await PowerOffChassisAsync(chassis);
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

    private async Task InitializeChassisAsync(IChassisManager chassis)
    {
      var result = await chassis.ConnectableManager.InitializeAsync();
      if (result.Connect)
      {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[INIT] Инициализация шасси: {chassis.Name} [ОК]");
      }
      else
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[INIT] Инициализация шасси: {chassis.Name} [{result.Answer}]");
      }
      Console.ResetColor();
      return;
    }

    private async Task PowerOnChassisAsync(IChassisManager chassis)
    {
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine($"[POWER ON] Питание включено на шасси: {chassis.Name}");
      Console.ResetColor();
      await chassis.PowerManager.StartPowerAsync();
      return;
    }

    private async Task PowerOffChassisAsync(IChassisManager chassis)
    {
      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine($"[POWER OFF] Питание отключено на шасси: {chassis.Name}");
      Console.ResetColor();
      await chassis.PowerManager.StopPowerAsync();
      return;
    }
  }
}
