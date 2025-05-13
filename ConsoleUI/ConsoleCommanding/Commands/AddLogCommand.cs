using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleUI.ConsoleCommanding.Core;

namespace ConsoleUI.ConsoleCommanding.Commands
{
  public class AddLogCommand : ICommand
  {
    public string Name => "add-log";

    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
      if (args.Length < 1)
      {
        context.Console.WriteLine("Ошибка: необходимо указать сообщение лога.");
        return;
      }

      var message = string.Join(" ", args);
      context.Console.WriteLine($"Лог добавлен: {message}");
      await Task.CompletedTask;
    }
  }
}
