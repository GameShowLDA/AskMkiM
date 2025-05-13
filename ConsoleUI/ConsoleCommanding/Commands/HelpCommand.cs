using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUI.ConsoleCommanding.Commands
{
  public class HelpCommand : ICommand
  {
    private readonly IEnumerable<ICommand> _allCommands;

    public string Name => "help";

    public HelpCommand(IEnumerable<ICommand> allCommands)
    {
      _allCommands = allCommands;
    }

    public Task ExecuteAsync(string[] args, CommandContext context)
    {
      context.WriteLine("Доступные команды:");
      foreach (var cmd in _allCommands.OrderBy(c => c.Name))
        context.WriteLine($"  - {cmd.Name}");

      return Task.CompletedTask;
    }
  }
}
