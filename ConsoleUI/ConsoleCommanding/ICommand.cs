using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUI.ConsoleCommanding
{
  public interface ICommand
  {
    string Name { get; }
    Task ExecuteAsync(string[] args, CommandContext context);
  }
}
