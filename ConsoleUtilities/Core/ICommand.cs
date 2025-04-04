using ConsoleUtilities.Models;

namespace ConsoleUtilities.Core
{
  /// <summary>
  /// Интерфейс, представляющий консольную команду.
  /// </summary>
  public interface ICommand
  {
    /// <summary>
    /// Имя команды, используемое для её вызова.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Выполняет команду с заданными аргументами и контекстом.
    /// </summary>
    /// <param name="args">Аргументы, переданные команде.</param>
    /// <param name="context">Контекст, содержащий состояние и зависимости.</param>
    /// <returns>Задача, представляющая асинхронную операцию выполнения.</returns>
    Task ExecuteAsync(string[] args, CommandContext context);
  }
}
