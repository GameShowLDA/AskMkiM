using ConsoleUtilities.Core;
using System.Text;

namespace ConsoleUtilities.Models
{
  /// <summary>
  /// Представляет контекст выполнения команды.
  /// Хранит зависимости и текущее состояние.
  /// </summary>
  public class CommandContext
  {
    /// <summary>
    /// Событие изменения режима администратора.
    /// </summary>
    public event EventHandler<bool> AdminModeChanged;

    /// <summary>
    /// Сервис вывода в консоль.
    /// </summary>
    public IConsoleWriter Console { get; }

    /// <summary>
    /// Буфер вывода консоли.
    /// </summary>
    public StringBuilder ConsoleLogBuffer { get; }

    /// <summary>
    /// Указывает, активен ли режим администратора.
    /// </summary>
    public bool IsAdmin { get; set; }

    /// <summary>
    /// Создаёт новый экземпляр <see cref="CommandContext"/>.
    /// </summary>
    /// <param name="console">Консольный вывод.</param>
    /// <param name="logBuffer">Буфер лога консоли.</param>
    public CommandContext(IConsoleWriter console, StringBuilder logBuffer)
    {
      Console = console ?? throw new ArgumentNullException(nameof(console));
      ConsoleLogBuffer = logBuffer ?? new StringBuilder(); // safety first
    }
  }
}
