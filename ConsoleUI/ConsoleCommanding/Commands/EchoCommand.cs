using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUI.ConsoleCommanding.Commands
{
  public class EchoCommand : ICommand
  {
    public string Name => "echo";
    public Task ExecuteAsync(string[] args, CommandContext context)
    {
      var output = string.Join(" ", args);
      context.WriteLine(output);
      return Task.CompletedTask;
    }
  }
}
