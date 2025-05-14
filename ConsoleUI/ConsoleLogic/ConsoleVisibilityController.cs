using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleUI.ConsoleUI;

namespace ConsoleUI.ConsoleLogic
{
  public static class ConsoleVisibilityController
  {
    private static ConsoleOverlay? _consoleWindow;

    public static void ToggleConsole()
    {
      if (_consoleWindow == null || !_consoleWindow.IsVisible)
      {
        _consoleWindow = new ConsoleOverlay();
        _consoleWindow.Show();
      }
      else
      {
        _consoleWindow.Hide();
      }
    }
  }

}
