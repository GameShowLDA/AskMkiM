using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleUI.ConsoleCommanding.Core;

namespace ConsoleUI.ConsoleCommanding.Commands
{
  public class HelpCommand : ICommand
  {
    private readonly IEnumerable<ICommand> _commands;

    public string Name => "help";

    public HelpCommand(IEnumerable<ICommand> commands)
    {
      _commands = commands;
    }

    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
      context.Console.WriteLine("Список доступных команд:");
      foreach (var command in _commands.OrderBy(c => c.Name))
      {
        context.Console.WriteLine($"  • {command.Name}");
      }

      await Task.CompletedTask;
    }
  }
}
