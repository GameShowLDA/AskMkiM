using ConsoleUI.ConsoleCommanding.Core;

namespace ConsoleUI.ConsoleCommanding.Models
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
