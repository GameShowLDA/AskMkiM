using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandExecutor.Execution;

namespace ControlCommandExecutor.Executors
{
  /// <summary>
  /// Интерфейс исполнителя команды контроля.
  /// </summary>
  public interface ICommandExecutor
  {
    string Mnemonic { get; }

    /// <summary>
    /// Выполняет команду на основе предоставленного контекста.
    /// </summary>
    /// <param name="context">Контекст выполнения команды.</param>
    /// <returns>Задача выполнения.</returns>
    Task ExecuteAsync(CommandExecutionContext context);
  }
}
