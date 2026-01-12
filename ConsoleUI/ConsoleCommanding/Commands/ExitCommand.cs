using ConsoleUI.ConsoleCommanding.Core;
using System.Windows;

namespace ConsoleUI.ConsoleCommanding.Commands
{
  public class ExitCommand : ICommand
  {
    public string Name => "exit";

    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
      context.Console.WriteLine("Завершение работы...");
      Application.Current.Shutdown();
      await Task.CompletedTask;
    }
  }
}
