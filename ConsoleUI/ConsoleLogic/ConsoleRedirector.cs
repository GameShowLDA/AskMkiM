using System.IO;
using System.Text;

namespace ConsoleUI.ConsoleLogic
{
  public class ConsoleRedirector : TextWriter
  {
    private readonly TextWriter _originalConsoleOut = Console.Out;
    private readonly StringBuilder _buffer = new();

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
      _originalConsoleOut.Write(value);

      if (value == '\r') return;

      if (value == '\n')
      {
        FlushBuffer();
        return;
      }

      _buffer.Append(value);
    }

    public override void WriteLine(string? value)
    {
      _originalConsoleOut.WriteLine(value);
      ConsoleTextManager.Instance.Append(value ?? string.Empty);
    }

    private void FlushBuffer()
    {
      if (_buffer.Length == 0) return;
      ConsoleTextManager.Instance.Append(_buffer.ToString());
      _buffer.Clear();
    }
  }
}
