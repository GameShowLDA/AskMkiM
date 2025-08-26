using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ControlCommandAnalyser.Model;
using Utilities.Interface;
using Utilities.TextEditor;

namespace ControlCommandExecutor.Execution
{
  /// <summary>
  /// Контекст выполнения команды, содержащий модель команды и инструменты вывода.
  /// </summary>
  public class CommandExecutionContext
  {
    public BaseCommandModel Command { get; }
    public IUserMessageService Console { get; }
    public ITextEditorAdapter TranslationControl { get; }

    public CommandExecutionManager CommandExecutionManager { get; }


    /// <summary>
    /// Делегат для перехода к команде по номеру (метке). 
    /// Заполняется менеджером выполнения команд.
    /// </summary>
    public Action<string> JumpToCommandNumber { get; set; }

    /// <summary>
    /// Дополнительные данные, общие между исполнителями.
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = new();

    public CommandExecutionContext(CommandExecutionManager commandExecutionManager, BaseCommandModel command, IUserMessageService console, ITextEditorAdapter editorAdapter)
    {
      Command = command;
      Console = console;
      TranslationControl = editorAdapter;
      CommandExecutionManager = commandExecutionManager;
    }
  }
}
