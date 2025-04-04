using System.Text;

namespace ConsoleUtilities.Services
{
  /// <summary>
  /// Поток вывода, который записывает как в оригинальную консоль, так и в буфер.
  /// </summary>
  public class ConsoleWriter : TextWriter
  {
    private readonly TextWriter _original;
    private readonly StringBuilder _logBuffer;

    public ConsoleWriter(TextWriter original, StringBuilder logBuffer)
    {
      _original = original;
      _logBuffer = logBuffer;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
      _original.Write(value);
      _logBuffer.Append(value);
    }

    public override void Write(string? value)
    {
      _original.Write(value);
      _logBuffer.Append(value);
    }

    public override void WriteLine(string? value)
    {
      _original.WriteLine(value);
      _logBuffer.AppendLine(value);
    }
  }
}
