using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleUI.ConsoleCommanding.Commands;
using ConsoleUI.ConsoleCommanding.Core;

namespace ConsoleUI.ConsoleCommanding
{
  public class CommandRouter
  {
    private readonly Dictionary<string, ICommand> _commands;

    public CommandRouter(IEnumerable<ICommand> commands)
    {
      _commands = commands.ToDictionary(c => c.Name.ToLower());
    }

    public async Task RouteAsync(string input, CommandContext context)
    {
      var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length == 0) return;

      var commandName = parts[0].ToLower();
      var args = parts.Skip(1).ToArray();

      if (_commands.TryGetValue(commandName, out var command))
        await command.ExecuteAsync(args, context);
      else
        await new UnknownCommand().ExecuteAsync(parts, context);
    }

    public IEnumerable<string> GetCommandNames() => _commands.Keys;
  }
}
