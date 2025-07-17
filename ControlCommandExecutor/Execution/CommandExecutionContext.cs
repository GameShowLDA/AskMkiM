using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;
using Utilities.Interface;

namespace ControlCommandExecutor.Execution
{
  /// <summary>
  /// Контекст выполнения команды, содержащий модель команды и инструменты вывода.
  /// </summary>
  public class CommandExecutionContext
  {
    public BaseCommandModel Command { get; }
    public IUserMessageService Console { get; }

    /// <summary>
    /// Дополнительные данные, общие между исполнителями.
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = new();

    public CommandExecutionContext(BaseCommandModel command, IUserMessageService console)
    {
      Command = command;
      Console = console;
    }
  }
}
