using ConsoleUtilities.Core;
using ConsoleUtilities.Models;

namespace ConsoleUtilities.Commands
{
  /// <summary>
  /// Очищает консольный вывод.
  /// </summary>
  public class ClearCommand : ICommand
  {
    /// <inheritdoc />
    public string Name => "clear";

    /// <inheritdoc />
    public Task ExecuteAsync(string[] args, CommandContext context)
    {
      context.Console.Clear();
      return Task.CompletedTask;
    }
  }
}
