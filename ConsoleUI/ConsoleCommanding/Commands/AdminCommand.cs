using System;
using System.Threading.Tasks;
using ConsoleUI.ConsoleCommanding.Core;

namespace ConsoleUI.ConsoleCommanding.Commands
{
  public class AdminCommand : ICommand
  {
    public static bool IsAdminModeEnabled { get; private set; }

    public static event EventHandler<bool> AdminModeChanged;

    public string Name => "admin";

    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
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
      AdminModeChanged?.Invoke(this, enable);

      context.Console.WriteLine(enable
        ? "Режим администратора включён."
        : "Режим администратора отключён.");
    }
  }
}
