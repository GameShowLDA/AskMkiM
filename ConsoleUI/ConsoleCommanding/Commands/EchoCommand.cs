using ConsoleUI.ConsoleCommanding.Core;

namespace ConsoleUI.ConsoleCommanding.Commands
{
  public class EchoCommand : ICommand
  {
    public string Name => "echo";

    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
      var message = string.Join(" ", args);
      context.Console.WriteLine(message);
      await Task.CompletedTask;
    }
  }
}
