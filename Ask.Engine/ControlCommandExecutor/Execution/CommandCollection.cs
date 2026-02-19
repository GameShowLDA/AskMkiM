using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Engine.ControlCommandExecutor.Execution
{
  /// <summary>
  /// Коллекция команд управляющей программы
  /// с поддержкой поиска по номеру.
  /// </summary>
  internal sealed class CommandCollection
  {
    /// <summary>
    /// Внутренний список команд управляющей программы.
    /// </summary>
    private readonly List<BaseCommandModel> _commands;

    public CommandCollection(List<BaseCommandModel> commands)
    {
      _commands = commands;
    }

    /// <summary>
    /// Возвращает количество команд в коллекции.
    /// </summary>
    public int Count => _commands.Count;

    /// <summary>
    /// Предоставляет доступ к команде по индексу.
    /// </summary>
    /// <param name="index">
    /// Индекс команды в коллекции.
    /// </param>
    /// <returns>
    /// Модель команды управляющей программы.
    /// </returns>
    public BaseCommandModel this[int index] => _commands[index];

    /// <summary>
    /// Возвращает снимок текущего списка команд.
    /// </summary>
    public IReadOnlyList<BaseCommandModel> Snapshot()
    {
      return _commands.ToArray();
    }

    /// <summary>
    /// Возвращает индекс команды в коллекции.
    /// </summary>
    public int IndexOf(BaseCommandModel command)
    {
      return _commands.IndexOf(command);
    }

    /// <summary>
    /// Выполняет поиск команды по её номеру.
    /// </summary>
    /// <param name="number">
    /// Номер команды, указанный в управляющей программе.
    /// </param>
    /// <returns>
    /// Модель команды, если команда найдена;
    /// иначе <c>null</c>.
    /// </returns>
    public BaseCommandModel? FindByNumber(int number)
    {
      return _commands.FirstOrDefault(c =>
          int.TryParse(c.CommandNumber, out var n) && n == number);
    }
  }
}
