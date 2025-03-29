using AppManager;
using AppManager.DataBase;
using AppManager.DataBase.Models;
using Microsoft.EntityFrameworkCore;
using NewCore.Device;

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
        Console.WriteLine("2. Добавить тестовые данные");
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

          case 2:
            Task.Run(AddRandomDataAsync).Wait();
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
      using var dbContext = AppManager.Config.SystemStateManager.Context;

      var chassisManagers = await dbContext.ChassisManagers.ToListAsync();
      var relaySwitchModules = await dbContext.RelaySwitchModules.ToListAsync();
      var powerSourceModules = await dbContext.PowerSourceModules.ToListAsync();
      var switchingDevices = await dbContext.SwitchingDevices.ToListAsync();
      var precisionMeters = await dbContext.PrecisionMeters.ToListAsync();
      var fastMeters = await dbContext.FastMeters.ToListAsync();
      var breakdownTesters = await dbContext.BreakdownTesters.ToListAsync();

      Console.WriteLine("\nСписок устройств:");
      Console.WriteLine($"Менеджеры шасси: {chassisManagers.Count}");
      Console.WriteLine($"Модули коммутации реле: {relaySwitchModules.Count}");
      Console.WriteLine($"Модули источников питания: {powerSourceModules.Count}");
      Console.WriteLine($"Устройства коммутации: {switchingDevices.Count}");
      Console.WriteLine($"Точные измерители: {precisionMeters.Count}");
      Console.WriteLine($"Быстрые измерители: {fastMeters.Count}");
      Console.WriteLine($"Пробойные установки: {breakdownTesters.Count}");
    }

    /// <summary>
    /// Удаляет все данные из базы данных.
    /// </summary>
    private static async Task DeleteAllDataAsync()
    {
      DbContextOptionsBuilder<AppDbContext> optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite($"Data Source={FileLocations.ConfigFilePath}");

      using var dbContext = new AppDbContext(optionsBuilder.Options);
      Console.WriteLine("Удаление всех данных...");

      dbContext.ChassisManagers.RemoveRange(dbContext.ChassisManagers);
      dbContext.RelaySwitchModules.RemoveRange(dbContext.RelaySwitchModules);
      dbContext.PowerSourceModules.RemoveRange(dbContext.PowerSourceModules);
      dbContext.SwitchingDevices.RemoveRange(dbContext.SwitchingDevices);
      dbContext.PrecisionMeters.RemoveRange(dbContext.PrecisionMeters);
      dbContext.FastMeters.RemoveRange(dbContext.FastMeters);
      dbContext.BreakdownTesters.RemoveRange(dbContext.BreakdownTesters);

      await dbContext.SaveChangesAsync();
      Console.WriteLine("Все данные удалены.");
    }

    /// <summary>
    /// Добавляет случайные данные в базу данных.
    /// </summary>
    private static async Task AddRandomDataAsync()
    {
      DbContextOptionsBuilder<AppDbContext> optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite($"Data Source={FileLocations.ConfigFilePath}");

      using var dbContext = new AppDbContext(optionsBuilder.Options);
      Console.WriteLine("Добавление тестовых данных...");

      var random = new Random();

      dbContext.ChassisManagers.Add(new ChassisManagerEntity { Name = "Шасси 1", Description = "Тестовое шасси", Number = random.Next(1, 100) });
      dbContext.RelaySwitchModules.Add(new RelaySwitchModuleEntity { Name = "Модуль реле 1", Description = "Тестовый модуль реле", Number = random.Next(1, 100), NumberChassis = 1, PointCount = 16 });
      dbContext.PowerSourceModules.Add(new PowerSourceModuleEntity { Name = "Источник питания 1", Description = "Тестовый источник", Number = random.Next(1, 100) });
      dbContext.SwitchingDevices.Add(new SwitchingDeviceEntity { Name = "Коммутационное устройство 1", Description = "Тестовое устройство", Number = random.Next(1, 100), NumberChassis = 1 });
      dbContext.PrecisionMeters.Add(new PrecisionMeterEntity { Name = "Точный измеритель 1", Description = "Тестовый измеритель", Number = random.Next(1, 100) });
      dbContext.FastMeters.Add(new FastMeterEntity { Name = "Быстрый измеритель 1", Description = "Тестовый измеритель", Number = random.Next(1, 100) });
      dbContext.BreakdownTesters.Add(new BreakdownTesterEntity { Name = "Пробойная установка 1", Description = "Тестовая установка", Number = random.Next(1, 100), NumberChassis = 1 });

      await dbContext.SaveChangesAsync();
      Console.WriteLine("Тестовые данные добавлены.");
    }
  }
}
