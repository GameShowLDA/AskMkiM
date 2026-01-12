namespace ConsoleUI.ConsoleCommanding.Core
{
  public interface ICommand
  {
    string Name { get; }
    Task ExecuteAsync(string[] args, CommandContext context);
  }
}
