using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleUI.ConsoleCommanding.Core;
using ConsoleUI.ConsoleLogic;

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
