using ConsoleUI.ConsoleCommanding.Core;

namespace ConsoleUI.ConsoleCommanding.Commands
{
  public class LogsCommand : ICommand
  {
    public string Name => "logs";

    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
      context.Console.WriteLine("Логи (пример):");
      context.Console.WriteLine("[INFO] Программа запущена.");
      context.Console.WriteLine("[WARN] Тестовое предупреждение.");
      context.Console.WriteLine("[ERROR] Ошибка подключения.");
      await Task.CompletedTask;
    }
  }
}
