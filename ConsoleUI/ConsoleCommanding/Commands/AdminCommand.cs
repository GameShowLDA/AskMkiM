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
    public static event EventHandler<bool> AdminModeChanged;
    public static event EventHandler<bool> PauseInStopChanged;

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
      context.Console.WriteLine("3. Включить останов по ошибке");
      context.Console.WriteLine("4. Выключить останов по ошибке");
      context.Console.WriteLine("0. Выход");

      string input = await context.Console.ReadLineAsync();
      if (!int.TryParse(input, out int choice) || choice < 0 || choice > 4)
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
        case 3:
          OffPause(true, context);
          break;
        case 4:
          OffPause(false, context);
          break;
        default:
          context.Console.WriteLine("Выход без изменений.");
          break;
      }
    }

    private void SetAdminMode(bool enable, CommandContext context)
    {
      AdminModeChanged?.Invoke(null, enable);

      context.Console.WriteLine(enable
        ? "Режим администратора включён."
        : "Режим администратора отключён.");
    }

    private void OffPause(bool enable, CommandContext context)
    {
      PauseInStopChanged?.Invoke(null, enable);

      context.Console.WriteLine(enable
        ? "Включен режим остановки по ошибке."
        : "Выключен режим остановки по ошибке.");
    }
  }
}
