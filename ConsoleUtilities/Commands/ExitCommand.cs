using ConsoleUtilities.Core;
using ConsoleUtilities.Models;

namespace ConsoleUtilities.Commands
{
  /// <summary>
  /// Завершает работу приложения.
  /// </summary>
  public class ExitCommand : ICommand
  {
    /// <inheritdoc />
    public string Name => "exit";

    /// <inheritdoc />
    public Task ExecuteAsync(string[] args, CommandContext context)
    {
      context.Console.WriteLine("Выход из приложения...");
      Environment.Exit(0); // да, мы просто уходим
      return Task.CompletedTask;
    }
  }
}
