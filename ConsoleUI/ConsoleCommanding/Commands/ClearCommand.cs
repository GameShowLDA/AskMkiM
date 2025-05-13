using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleUI.ConsoleLogic;

namespace ConsoleUI.ConsoleCommanding.Commands
{
  public class ClearCommand : ICommand
  {
    public string Name => "clear";

    public Task ExecuteAsync(string[] args, CommandContext context)
    {
      // ConsoleTextManager.Instance.Clear(); // Ты должен реализовать метод Clear
      return Task.CompletedTask;
    }
  }
}
