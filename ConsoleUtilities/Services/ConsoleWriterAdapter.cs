using ConsoleUtilities.Core;
using System.Text;

namespace ConsoleUtilities.Services
{
  /// <summary>
  /// Реализация <see cref="IConsoleWriter"/>, сохраняющая вывод в буфер.
  /// </summary>
  public class ConsoleWriterAdapter : IConsoleWriter
  {
    private readonly StringBuilder _logBuffer;

    public ConsoleWriterAdapter(StringBuilder logBuffer)
    {
      _logBuffer = logBuffer ?? throw new ArgumentNullException(nameof(logBuffer));
    }

    public void Write(string message)
    {
      Console.Write(message);
      _logBuffer.Append(message);
    }

    public void WriteLine(string message)
    {
      Console.WriteLine(message);
      _logBuffer.AppendLine(message);
    }

    public void Clear()
    {
      Console.Clear();
      _logBuffer.Clear();
    }
  }
}
