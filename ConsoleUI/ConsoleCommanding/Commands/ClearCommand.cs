using ConsoleUI.ConsoleCommanding.Core;

namespace ConsoleUI.ConsoleCommanding.Commands
{
  public class ClearCommand : ICommand
  {
    public string Name => "clear";

    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
      context.Console.Clear();
      await Task.CompletedTask;
    }
  }
}
