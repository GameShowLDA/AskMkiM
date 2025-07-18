using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;
using ControlCommandExecutor.Executors;
using Utilities.Interface;
using Utilities.TextEditor;

namespace ControlCommandExecutor.Execution
{
  /// <summary>
  /// Основной исполнитель команд контроля.
  /// </summary>
  public class CommandExecutionManager
  {
    private readonly Dictionary<string, ICommandExecutor> _executors = new();
    private readonly IUserMessageService _console;
    private readonly ITextEditorAdapter tranlationControl;

    public List<BaseCommandModel> CommandsToExecute { get; set; } = new();

    public CommandExecutionManager(IUserMessageService console, ITextEditorAdapter textEditor, List<BaseCommandModel> ControlProgram)
    {
      _console = console;
      CommandsToExecute = ControlProgram;
      tranlationControl = textEditor;
      RegisterExecutors();
    }

    /// <summary>
    /// Выполняет все команды по очереди.
    /// </summary>
    public async Task ExecuteAllAsync()
    {
      foreach (var command in CommandsToExecute)
      {
        await ExecuteOneAsync(command);
      }
    }

    /// <summary>
    /// Выполняет одну команду по предоставленной модели.
    /// </summary>
    public async Task ExecuteOneAsync(BaseCommandModel command)
    {
      if (_executors.TryGetValue(command.Mnemonic, out var executor))
      {
        var context = new CommandExecutionContext(command, _console, tranlationControl);
        await executor.ExecuteAsync(context);
      }
      else
      {
        await _console.ShowMessageAsync(new Utilities.Models.ShowMessageModel("Неизвестная команда", message: command.Mnemonic, type: Utilities.Models.ShowMessageModel.MessageType.Error));
      }
    }

    /// <summary>
    /// Регистрирует исполнителей команд.
    /// </summary>
    private void RegisterExecutors()
    {
      var executorInterface = typeof(ICommandExecutor);
      var executorTypes = Assembly.GetExecutingAssembly()
          .GetTypes()
          .Where(t => !t.IsAbstract && !t.IsInterface && executorInterface.IsAssignableFrom(t));

      foreach (var type in executorTypes)
      {
        var instance = (ICommandExecutor)Activator.CreateInstance(type)!;
        _executors[instance.Mnemonic] = instance;
      }
    }
  }
}
