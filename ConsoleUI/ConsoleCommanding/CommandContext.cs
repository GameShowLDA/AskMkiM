using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleUI.ConsoleCommanding.Core;

namespace ConsoleUI.ConsoleCommanding
{
  public class CommandContext
  {
    public IConsoleWriter Console { get; }

    public CommandContext(IConsoleWriter console)
    {
      Console = console;
    }
  }
}
