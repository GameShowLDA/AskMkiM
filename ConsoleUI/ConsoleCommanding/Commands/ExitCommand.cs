using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ConsoleUI.ConsoleCommanding.Core;

namespace ConsoleUI.ConsoleCommanding.Commands
{
  public class ExitCommand : ICommand
  {
    public string Name => "exit";

    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
      context.Console.WriteLine("Завершение работы...");
      Application.Current.Shutdown();
      await Task.CompletedTask;
    }
  }
}
