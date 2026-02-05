using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Executors.Interface;
using System.Reflection;

namespace Ask.Engine.ControlCommandExecutor.Execution
{
  /// <summary>
  /// Основной исполнитель команд контроля.
  /// </summary>
  public class CommandExecutionManager
  {
    private readonly Dictionary<string, ICommandExecutor> _executors = new(StringComparer.OrdinalIgnoreCase);
    private readonly IUserInteractionService _console;
    private readonly ITextEditorAdapter _textEditor;
    private ProtocolModel _protocolModel = new ProtocolModel();
    private readonly string? _opkFilePath;

    /// <summary>
    /// Событие, которое вызывается при изменении состояния блокировки.
    /// </summary>
    public event Action<ErrorItem> AddError;
    public event Action ClearError;


    /// <summary>
    /// Флаг, указывающий, активно ли питание системы.
    /// </summary>
    public void ClearErrorsMethod()
    {
      ClearError?.Invoke();
    }

    public void AddErrorMethod(ErrorItem errorItem)
    {
      AddError?.Invoke(errorItem);
    }

    public List<BaseCommandModel> CommandsToExecute { get; set; } = new();

    public CommandExecutionManager(IUserInteractionService console, ITextEditorAdapter textEditor, List<BaseCommandModel> ControlProgram, string? opkFilePath)
    {
      _console = console;
      CommandsToExecute = ControlProgram;
      _textEditor = textEditor;
      _opkFilePath = opkFilePath;
      RegisterExecutors();

      EventAggregator.Subscribe<BreakpointEvents.BreakpointSet>(e => BreakpointSet(e));
      EventAggregator.Subscribe<BreakpointEvents.BreakpointRemoved>(e => BreakpointRemoved(e));
    }

    /// <summary>
    /// Выполняет все команды по очереди.
    /// </summary>
    public async Task ExecuteAllAsync()
    {
      int i = 0;
      while (i < CommandsToExecute.Count)
      {
        var command = CommandsToExecute[i];

        var context = new CommandExecutionContext(this, command, _console, _textEditor, _opkFilePath)
        {
          JumpToCommandNumber = (number) =>
          {
            int newIndex = CommandsToExecute.FindIndex(cmd => cmd.CommandNumber == number);
            if (newIndex >= 0)
            {
              i = newIndex - 1;
            }
          }
        };

        if (_executors.TryGetValue(command.Mnemonic, out var executor))
        {
          await executor.ExecuteAsync(context, _protocolModel);
        }
        else
        {
          await _console.ShowMessageAsync(new ShowMessageModel("Неизвестная команда", message: command.Mnemonic, type: ShowMessageModel.MessageType.Error));
        }

        i++;
      }
    }

    /// <summary>
    /// Выполняет одну команду по предоставленной модели.
    /// </summary>
    public async Task ExecuteOneAsync(BaseCommandModel command)
    {
      if (_executors.TryGetValue(command.Mnemonic, out var executor))
      {
        var context = new CommandExecutionContext(this, command, _console, _textEditor, _opkFilePath);
        await executor.ExecuteAsync(context, _protocolModel);
      }
      else
      {
        await _console.ShowMessageAsync(new ShowMessageModel("Неизвестная команда", message: command.Mnemonic, type: ShowMessageModel.MessageType.Error));
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

    /// <summary>
    /// Пропускает команды до указанного номера и продолжает выполнение с неё (включительно или после неё).
    /// </summary>
    public async Task JumpToCommandAndExecuteAsync(string commandNumber)
    {
      int index = CommandsToExecute.FindIndex(cmd => cmd.CommandNumber == commandNumber);

      if (index < 0)
      {
        await _console.ShowMessageAsync(new ShowMessageModel(
            $"Команда с номером {commandNumber} не найдена.",
            message: "",
            type: ShowMessageModel.MessageType.Error));
        return;
      }

      for (int i = index; i < CommandsToExecute.Count; i++)
      {
        await ExecuteOneAsync(CommandsToExecute[i]);
      }
    }

    private void BreakpointSet(BreakpointEvents.BreakpointSet obj)
    {
      var model = GetCommandByNumber(obj.CommandNumber);
      if (model == null)
        return;

      model.HasBreakpoint = true;
    }

    private void BreakpointRemoved(BreakpointEvents.BreakpointRemoved obj)
    {
      var model = GetCommandByNumber(obj.CommandNumber);
      if (model == null)
        return;

      model.HasBreakpoint = false;
    }

    private BaseCommandModel? GetCommandByNumber(int commandNumber)
    {
      return CommandsToExecute
          .FirstOrDefault(x =>
              int.TryParse(x.CommandNumber, out var num) &&
              num == commandNumber);
    }
  }
}
