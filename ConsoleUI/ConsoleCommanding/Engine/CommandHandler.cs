using ConsoleUI.ConsoleCommanding.Core;

namespace ConsoleUI.ConsoleCommanding.Engine
{
  public class CommandHandler : ICommandHandler
  {
    private readonly Dictionary<string, ICommand> _commandMap;
    private readonly ICommand _unknown;

    public CommandHandler(IEnumerable<ICommand> commands)
    {
      _commandMap = commands.ToDictionary(c => c.Name.ToLowerInvariant());
      _unknown = commands.FirstOrDefault(c => c.Name == "unknown")
                 ?? throw new InvalidOperationException("UnknownCommand не найден.");
    }

    public async Task HandleAsync(string input, CommandContext context)
    {
      if (string.IsNullOrWhiteSpace(input))
        return;

      var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
      var commandName = parts[0].ToLowerInvariant();
      var args = parts.Skip(1).ToArray();

      if (_commandMap.TryGetValue(commandName, out var command))
      {
        await command.ExecuteAsync(args, context);
      }
      else
      {
        await _unknown.ExecuteAsync(parts, context);
      }
    }

    public IEnumerable<string> GetAllCommandNames()
    {
      return _commandMap.Keys;
    }

  }
}
