using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUI.ConsoleCommanding
{
  public class CommandContext
  {
    public Action<string> WriteLine { get; }

    public CommandContext(Action<string> writeLine)
    {
      WriteLine = writeLine;
    }
  }
}
