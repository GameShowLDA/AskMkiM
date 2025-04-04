using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleUtilities.Core;
using ConsoleUtilities.Interactor;
using ConsoleUtilities.Models;
using DataBaseConfiguration.Services;

namespace ConsoleUtilities.Commands
{
  internal class SendCommand : ICommand
  {
    public string Name => "send";

    private readonly Dictionary<int, IDeviceInteractor> _interactors = new()
        {
            { 1, new ChassisManagerInteractor() },
            { 2, new RelaySwitchInteractor() } 
        };

    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
      Console.WriteLine("=== Выбор типа устройства ===");
      Console.WriteLine("1. Менеджеры шасси");
      Console.WriteLine("2. Модули коммутации реле");
      Console.WriteLine("0. Назад");

      Console.Write("Введите номер: ");
      if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || !_interactors.ContainsKey(choice))
      {
        Console.WriteLine("Неверный выбор.");
        return;
      }

      await _interactors[choice].RunAsync();
    }
  }
}
