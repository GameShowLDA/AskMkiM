using ConsoleUI.ConsoleCommanding.Core;

namespace ConsoleUI.ConsoleCommanding.Commands
{
  public class UnknownCommand : ICommand
  {
    public string Name => "unknown";

    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
      var input = string.Join(" ", args);
      context.Console.WriteLine($"Неизвестная команда: {input}");
      context.Console.WriteLine("Введите 'help' для просмотра доступных команд.");
      await Task.CompletedTask;
    }
  }
}
