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
