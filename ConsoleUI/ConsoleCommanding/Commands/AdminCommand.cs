using System;
using System.Threading.Tasks;
using System.Windows;
using ConsoleUI.ConsoleCommanding.Core;
using ConsoleUI.ConsoleUI;

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

      ((ConsoleOverlay)Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is ConsoleOverlay))?.SetPasswordMode(true);
      string password = await context.Console.ReadLineAsync();

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
