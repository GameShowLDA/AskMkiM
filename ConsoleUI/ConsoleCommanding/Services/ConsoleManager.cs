using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleUI.ConsoleCommanding.Core;

namespace ConsoleUI.ConsoleCommanding.Services
{
  public class ConsoleManager
  {
    private readonly CmdExecutor _executor;
    private readonly IConsoleWriter _writer;

    public ConsoleManager(IConsoleWriter writer, ICommandHandler handler)
    {
      _writer = writer;
      _executor = new CmdExecutor(handler);
    }

    public async Task RunCommandAsync(string input)
    {
      var context = new CommandContext(_writer);
      await _executor.ExecuteAsync(input, context);
    }
  }
}
