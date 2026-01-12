using ConsoleUI.ConsoleCommanding.Core;

namespace ConsoleUI.ConsoleCommanding.Commands
{
  public class DelAdminCommand : ICommand
  {
    public string Name => "del-admin";

    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
      if (args.Length < 1)
      {
        context.Console.WriteLine("Ошибка: не указано имя пользователя.");
        return;
      }

      var user = args[0];
      context.Console.WriteLine($"Пользователь {user} удалён из списка администраторов.");
      await Task.CompletedTask;
    }
  }
}
