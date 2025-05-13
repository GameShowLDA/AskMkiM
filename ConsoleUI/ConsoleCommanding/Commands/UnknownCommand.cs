using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUI.ConsoleCommanding.Commands
{
  public class UnknownCommand : ICommand
  {
    public string Name => "unknown";

    public Task ExecuteAsync(string[] args, CommandContext context)
    {
      context.WriteLine("Неизвестная команда. Введите 'help' для списка.");
      return Task.CompletedTask;
    }
  }
}
