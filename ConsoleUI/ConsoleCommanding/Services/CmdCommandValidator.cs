using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUI.ConsoleCommanding.Services
{
  public class CmdCommandValidator
  {
    public bool IsValid(string input)
    {
      return !string.IsNullOrWhiteSpace(input);
    }
  }
}
