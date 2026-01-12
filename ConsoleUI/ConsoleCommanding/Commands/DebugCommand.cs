using ConsoleUI.ConsoleCommanding.Core;

namespace ConsoleUI.ConsoleCommanding.Commands
{
  public class DebugCommand : ICommand
  {
    public static event EventHandler<bool> DebugModeChanged;

    public string Name => "debug";

    public async Task ExecuteAsync(string[] args, CommandContext context)
    {

      context.Console.WriteLine("Режим отладки:");
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
          SetDebugMode(true, context);
          break;
        case 2:
          SetDebugMode(false, context);
          break;
        default:
          context.Console.WriteLine("Выход без изменений.");
          break;
      }
    }

    private void SetDebugMode(bool enable, CommandContext context)
    {
      DebugModeChanged?.Invoke(null, enable);

      context.Console.WriteLine(enable
        ? "Режим отладки включён."
        : "Режим отладки отключён.");
    }
  }
}
