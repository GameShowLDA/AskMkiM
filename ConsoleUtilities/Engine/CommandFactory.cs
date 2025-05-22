using ConsoleUtilities.Commands;
using ConsoleUtilities.Core;

namespace ConsoleUtilities.Engine
{
  /// <summary>
  /// Фабрика команд, предоставляющая реализацию команды по её имени.
  /// </summary>
  public class CommandFactory
  {
    private readonly Dictionary<string, ICommand> _commands;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="CommandFactory"/>.
    /// </summary>
    /// <param name="commands">Набор доступных команд, индексируемых по имени.</param>
    public CommandFactory(IEnumerable<ICommand> commands)
    {
      _commands = commands.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Получает команду по указанному имени. 
    /// Если команда не найдена, возвращает заглушку <see cref="UnknownCommand"/>.
    /// </summary>
    /// <param name="name">Имя команды, введённое пользователем.</param>
    /// <returns>Экземпляр команды, реализующей <see cref="ICommand"/>.</returns>
    public ICommand GetCommand(string name)
    {
      return _commands.TryGetValue(name, out var command)
          ? command
          : new UnknownCommand(name);
    }
  }
}
