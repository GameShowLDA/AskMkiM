using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUI.ConsoleCommanding.Core
{
  public interface ICommandHandler
  {
    Task HandleAsync(string input, CommandContext context);
  }
}
