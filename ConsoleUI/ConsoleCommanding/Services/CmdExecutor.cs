using ConsoleUI.ConsoleCommanding.Core;

namespace ConsoleUI.ConsoleCommanding.Services
{
  public class CmdExecutor
  {
    private readonly ICommandHandler _handler;
    private readonly CmdCommandValidator _validator;

    public CmdExecutor(ICommandHandler handler)
    {
      _handler = handler;
      _validator = new CmdCommandValidator();
    }

    public async Task ExecuteAsync(string input, CommandContext context)
    {
      if (_validator.IsValid(input))
      {
        await _handler.HandleAsync(input, context);
      }
      else
      {
        context.Console.WriteLine("Пустая или недопустимая команда.");
      }
    }
  }
}
