using System;
using System.Threading.Tasks;
using ConsoleUtilities.Core;
using ConsoleUtilities.Models;
using ConsoleUtilities.Services;
using DataBaseConfiguration.Models;
using DataBaseConfiguration.Services;

namespace ConsoleUtilities.Commands
{
  /// <summary>
  /// Команда для вывода данных об оборудовании в виде таблицы
  /// </summary>
  internal class DatabaseCommand : ICommand
  {
    public string Name => "database";

    /// <summary>
    /// Выполняет команду для отображения данных оборудования
    /// </summary>
    /// <param name="args">Аргументы команды</param>
    /// <param name="context">Контекст выполнения команды</param>
    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
      Console.WriteLine("=== Данные оборудований ===");

      while (true)
      {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("1. Менеджеры шасси");
        Console.WriteLine("2. Модули коммутации реле");
        Console.WriteLine("3. Источники напряжения и тока");
        Console.WriteLine("4. Устройства коммутации");
        Console.WriteLine("5. Точные измерители");
        Console.WriteLine("6. Быстрые измерители");
        Console.WriteLine("7. Пробойные установки");
        Console.WriteLine("8. Стойки");
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
            TableFormatter.DisplayTable(new ChassisManagerServices().GetAllEntities());
            break;

          case 2:
            TableFormatter.DisplayTable(new RelaySwitchModuleServices().GetAllEntities());
            break;
          case 3:
            TableFormatter.DisplayTable(new PowerSourceModuleServices().GetAllEntities());
            break;
          case 4:
            TableFormatter.DisplayTable(new SwitchingDeviceServices().GetAllEntities());
            break;
          case 5:
            TableFormatter.DisplayTable(new PrecisionMeterServices().GetAllEntities());
            break;
          case 6:
            TableFormatter.DisplayTable(new FastMeterServices().GetAllEntities());
            break;
          case 7:
            TableFormatter.DisplayTable(new BreakdownTesterServices().GetAllEntities());
            break;
          case 8:
            TableFormatter.DisplayTable(new RackServices().GetAllEntities());
            break;
          case 0:
            return;
        }
      }
    }
  }
}