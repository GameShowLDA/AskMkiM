using Ask.Core.Services.App;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandExecutor.Execution
{
  /// <summary>
  /// Основной исполнитель команд контроля.
  /// </summary>
  public class CommandExecutionManager
  {
    /// <summary>
    /// Реестр исполнителей команд, обеспечивающий получение исполнителя по мнемонике команды.
    /// </summary>
    private readonly CommandExecutorRegistry _executorRegistry;

    /// <summary>
    /// Коллекция команд управляющей программы, предназначенных для выполнения.
    /// </summary>
    private readonly CommandCollection _commands;

    /// <summary>
    /// Менеджер точек останова команд, синхронизирующий состояние breakpoint-ов с моделями команд.
    /// </summary>
    private readonly BreakpointManager _breakpointManager;

    /// <summary>
    /// Сервис взаимодействия с пользователем,
    /// используемый для вывода сообщений и уведомлений.
    /// </summary>
    private readonly IUserInteractionService _console;

    /// <summary>
    /// Адаптер текстового редактора,
    /// используемый для навигации и визуального отображения команд.
    /// </summary>
    private readonly ITextEditorAdapter _textEditor;

    /// <summary>
    /// Модель протокола выполнения команд,
    /// используемая для накопления результатов выполнения.
    /// </summary>
    private readonly ProtocolModel _protocolModel = new();

    /// <summary>
    /// Путь к файлу управляющей программы (ОПК),
    /// используемый в процессе выполнения команд.
    /// </summary>
    private readonly string? _opkFilePath;

    /// <summary>
    /// Признак выполнения аварийного запуска КЦ после исключения.
    /// Нужен для защиты от рекурсивного повторного входа.
    /// </summary>
    private bool _isExecutingEmergencyKsc;

    /// <summary>
    /// Событие добавления ошибки выполнения.
    /// Используется для уведомления внешних компонентов
    /// о возникновении ошибки.
    /// </summary>
    public event Action<ErrorItem> AddError;

    /// <summary>
    /// Событие очистки списка ошибок выполнения.
    /// </summary>
    public event Action ClearError;

    /// <summary>
    /// Вызывает событие очистки ошибок выполнения.
    /// </summary>
    public void ClearErrorsMethod() => ClearError?.Invoke();

    /// <summary>
    /// Вызывает событие добавления ошибки выполнения.
    /// </summary>
    /// <param name="errorItem">
    /// Информация об ошибке выполнения команды.
    /// </param>
    public void AddErrorMethod(ErrorItem errorItem) => AddError?.Invoke(errorItem);

    public List<BaseCommandModel> CommandsToExecute { get; set; } = new();

    public IReadOnlyList<BaseCommandModel> GetCommandsSnapshot() => _commands.Snapshot();

    public CommandExecutionManager(
     IUserInteractionService console,
     ITextEditorAdapter textEditor,
     List<BaseCommandModel> controlProgram,
     string? opkFilePath)
    {
      _console = console;
      _textEditor = textEditor;
      _opkFilePath = opkFilePath;
      CommandsToExecute = controlProgram;

      _commands = new CommandCollection(controlProgram);
      _executorRegistry = new CommandExecutorRegistry();
      _breakpointManager = new BreakpointManager(_commands);
      CommandsToExecute = _commands.GetAllCommands();
    }

    /// <summary>
    /// Выполняет все команды по очереди.
    /// </summary>
    public async Task ExecuteAllAsync()
    {
      int index = 0;

      while (index < _commands.Count)
      {
        var command = _commands[index];
        try
        {
          if (command.FormattedStartLineNumber >= 0)
          {
            _textEditor.SetActiveLine(command.FormattedStartLineNumber);
          }

          var selected = await BreakpointHandler.OnBreakpointHitAsync(command, _commands.Snapshot(), _console);
          if (selected == null)
          {
            break;
          }

          if (!ReferenceEquals(selected, command))
          {
            var selectedIndex = _commands.IndexOf(selected);
            if (selectedIndex >= 0)
            {
              index = selectedIndex;
              command = selected;
            }
          }

          var context = new CommandExecutionContext(
              this, command, _console, _textEditor, _opkFilePath);

          if (_executorRegistry.TryGet(command.Mnemonic, out var executor))
          {
            await executor.ExecuteAsync(context, _protocolModel);
          }
          else
          {
            await _console.ShowMessageAsync(
                new ShowMessageModel("Неизвестная команда",
                    message: command.Mnemonic,
                    type: ShowMessageModel.MessageType.Error));
          }
        }
        catch (Exception ex)
        {
          await ExecuteKscOnExceptionAsync(command, ex);
          throw;
        }

        index++;
      }
    }

    private async Task ExecuteKscOnExceptionAsync(BaseCommandModel failedCommand, Exception ex)
    {
      if (_isExecutingEmergencyKsc)
        return;

      _isExecutingEmergencyKsc = true;
      StepControlManager.EnableStepMode(false);
      try
      {
        await _console.ShowMessageAsync(
          new ShowMessageModel(
            "\r\nОшибка выполнения команды",
            message: $"Команда: {failedCommand.CommandNumber} {failedCommand.Mnemonic}. {ex.Message} Запускается аварийное выполнение КЦ.",
            type: ShowMessageModel.MessageType.Error)
          { IndentLevel = 3 });

        var kscCommand = _commands
          .Snapshot()
          .OfType<KscCommandModel>()
          .LastOrDefault();

        if (kscCommand == null)
          return;

        if (_executorRegistry.TryGet(kscCommand.Mnemonic, out var kscExecutor))
        {
          var kscContext = new CommandExecutionContext(
            this, kscCommand, _console, _textEditor, _opkFilePath);

          await kscExecutor.ExecuteAsync(kscContext, _protocolModel);
        }
      }
      catch (Exception kscEx)
      {
        await _console.ShowMessageAsync(
          new ShowMessageModel(
            "Ошибка аварийного выполнения КЦ",
            message: kscEx.Message,
            type: ShowMessageModel.MessageType.Error)
          { IndentLevel = 3 });
      }
      finally
      {
        _isExecutingEmergencyKsc = false;
      }
    }
  }
}
