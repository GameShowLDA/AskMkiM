using Ask.Core.Shared.Entity.Devices;
using Ask.DataBase.Engine.Static.Devices;
using DataBaseConfiguration;
using DataBaseConfiguration.Context;
using Microsoft.EntityFrameworkCore;

namespace TestConsole
{
  /// <summary>
  /// Класс для тестирования работы с базой данных.
  /// </summary>
  public static class DBTest
  {
    /// <summary>
    /// Запускает тестовые операции с базой данных.
    /// </summary>
    public static void Run()
    {
      Console.WriteLine("\n=== Тест базы данных ===");

      while (true)
      {
        Console.WriteLine("\nВыберите действие:");
        Console.WriteLine("1. Вывести список устройств");
        Console.WriteLine("3. Удалить все данные");
        Console.WriteLine("0. Назад");

        Console.Write("Введите номер действия: ");
        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > 3)
        {
          Console.WriteLine("Неверный выбор. Попробуйте снова.");
          continue;
        }

        switch (choice)
        {
          case 1:
            Task.Run(DisplayDevicesAsync).Wait();
            break;

          case 3:
            Task.Run(DeleteAllDataAsync).Wait();
            break;

          case 0:
            return;

          default:
            Console.WriteLine("Неверный выбор. Попробуйте снова.");
            break;
        }
      }

    }

    /// <summary>
    /// Отображает список всех устройств в базе данных.
    /// </summary>
    private static async Task DisplayDevicesAsync()
    {
      var chassisManagers = await ChassisManagers.GetAllAsync();
      var relaySwitchModules = await RelaySwitchModules.GetAllAsync();
      var powerSourceModules = await PowerSourceModules.GetAllAsync();
      var switchingDevices = await SwitchingDevices.GetAllAsync();
      var fastMeters = await FastMeters.GetAllAsync();
      var breakdownTesters = await BreakdownTesters.GetAllAsync();

      Console.WriteLine("\nСписок устройств:");
      Console.WriteLine($"Менеджеры шасси: {chassisManagers.Count}");
      Console.WriteLine($"Модули коммутации реле: {relaySwitchModules.Count}");
      Console.WriteLine($"Модули источников питания: {powerSourceModules.Count}");
      Console.WriteLine($"Устройства коммутации: {switchingDevices.Count}");
      Console.WriteLine($"Быстрые измерители: {fastMeters.Count}");
      Console.WriteLine($"Пробойные установки: {breakdownTesters.Count}");
    }

    /// <summary>
    /// Удаляет все данные из базы данных.
    /// </summary>
    private static async Task DeleteAllDataAsync()
    {
      Console.WriteLine("Удаление всех данных...");

      await ChassisManagers.DeleteAllAsync();
      await RelaySwitchModules.DeleteAllAsync();
      await PowerSourceModules.DeleteAllAsync();
      await SwitchingDevices.DeleteAllAsync();
      await FastMeters.DeleteAllAsync();
      await BreakdownTesters.DeleteAllAsync();
      Console.WriteLine("Все данные удалены.");
    }
  }
}
