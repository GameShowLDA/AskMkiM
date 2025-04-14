using ConsoleUtilities.Core;
using ConsoleUtilities.Models;

namespace ConsoleUtilities.Commands
{
  /// <summary>
  /// Отображает список всех доступных команд.
  /// </summary>
  public class HelpCommand : ICommand
  {
    /// <inheritdoc />
    public string Name => "help";

    private readonly IEnumerable<ICommand> _availableCommands;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="HelpCommand"/>.
    /// </summary>
    /// <param name="availableCommands">Список всех зарегистрированных команд.</param>
    public HelpCommand(IEnumerable<ICommand> availableCommands)
    {
      _availableCommands = availableCommands;
    }

    /// <inheritdoc />
    public Task ExecuteAsync(string[] args, CommandContext context)
    {
      context.Console.WriteLine("Доступные команды:");

      foreach (var cmd in _availableCommands.OrderBy(c => c.Name))
      {
        context.Console.WriteLine($" - {cmd.Name}");
      }

      return Task.CompletedTask;
    }
  }
}
