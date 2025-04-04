using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleUtilities.Core;
using ConsoleUtilities.Models;
using ConsoleUtilities.Services;

namespace ConsoleUtilities.Commands
{
  public class AdminCommand : ICommand
  {
    static public event EventHandler<bool> AdminModeChanged;

    public string Name => "admin";
    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
      context.Console.WriteLine("Выберите функцию логов:");
      context.Console.WriteLine("1. Включить");
      context.Console.WriteLine("2. Выключить");
      context.Console.WriteLine("0. Выход");

      context.Console.Write("Ваш выбор: ");
      string input = Console.ReadLine();

      if (!int.TryParse(input, out int mode) || mode < 0 || mode > 2)
      {
        context.Console.WriteLine("Неверный выбор.");
        return;
      }

      if (mode == 0) return;
      SetAdminMode(mode == 1);
    }

    /// <summary>
    /// Устанавливает режим администратора.
    /// </summary>
    /// <param name="enable">
    /// Если <c>true</c>, включает режим администратора; если <c>false</c> — отключает.
    /// </param>
    private void SetAdminMode(bool enable)
    {
      if (enable)
      {
        AdminModeChanged?.Invoke(null, true);
      }
      else
      {
        AdminModeChanged?.Invoke(null, false);
      }
    }
  }
}
