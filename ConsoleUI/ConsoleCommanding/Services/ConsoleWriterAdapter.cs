using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ConsoleUI.ConsoleCommanding.Core;
using ConsoleUI.ConsoleLogic;
using ConsoleUI.ConsoleUI;

namespace ConsoleUI.ConsoleCommanding.Services
{
  public class ConsoleWriterAdapter : IConsoleWriter
  {
    public void WriteLine(string message) => ConsoleTextManager.Instance.Append(message);
    public void Clear() => ConsoleTextManager.Instance.Clear();

    public Task<string> ReadLineAsync()
    {
      var overlay = Application.Current.Windows.OfType<ConsoleOverlay>().FirstOrDefault();
      return overlay?.ReadLineAsync() ?? Task.FromResult(string.Empty);
    }
  }
}
