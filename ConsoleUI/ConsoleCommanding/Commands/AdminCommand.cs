using ConsoleUI.ConsoleCommanding.Core;
using ConsoleUI.ConsoleLogic;
using ConsoleUI.ConsoleUI;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace ConsoleUI.ConsoleCommanding.Commands
{
  public class AdminCommand : ICommand
  {
    public static bool IsAdminModeEnabled { get; private set; }

    public static event EventHandler<bool> AdminModeChanged;

    public string Name => "admin";

    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
      context.Console.WriteLine("Введите пароль:");

      ConsoleVisibilityController.SetPasswordMode(true);
      string password = await ConsoleVisibilityController.ReadLineAsync();


      if (password.ToLower() != "admin")
      {
        context.Console.WriteLine("|ERROR| Не верный пароль.");
        return;
      }

      context.Console.WriteLine("Режим администратора:");
      context.Console.WriteLine("1. Включить");
      context.Console.WriteLine("2. Выключить");
      context.Console.WriteLine("0. Выход");

      string input = await context.Console.ReadLineAsync();
      if (!int.TryParse(input, out int choice) || choice < 0 || choice > 2)
      {
        context.Console.WriteLine("Неверный выбор.");
        return;
      }

      switch (choice)
      {
        case 1:
          SetAdminMode(true, context);
          break;
        case 2:
          SetAdminMode(false, context);
          break;
        default:
          context.Console.WriteLine("Выход без изменений.");
          break;
      }
    }

    private void SetAdminMode(bool enable, CommandContext context)
    {
      IsAdminModeEnabled = enable;
      
      if (enable)
      {
        AdminModeChanged?.Invoke(null, true);
      }
      else
      {
        AdminModeChanged?.Invoke(null, false);
      }

      context.Console.WriteLine(enable
        ? "Режим администратора включён."
        : "Режим администратора отключён.");
    }
  }
}
