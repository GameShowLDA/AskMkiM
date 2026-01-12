namespace ConsoleUI.ConsoleCommanding.Core
{
  public interface ICommandHandler
  {
    Task HandleAsync(string input, CommandContext context);
  }
}
