using ConsoleUI.ConsoleCommanding.Core;
using ConsoleUI.ConsoleLogic;

namespace ConsoleUI.ConsoleCommanding.Services
{
  public class ConsoleWriterAdapter : IConsoleWriter
  {
    public void WriteLine(string message) => ConsoleTextManager.Instance.Append(message);
    public void Clear() => ConsoleTextManager.Instance.Clear();
    public Task<string> ReadLineAsync() => ConsoleVisibilityController.ReadLineAsync();
  }
}
