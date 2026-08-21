using Ask.Core.Services.App;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Exceptions;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.ExecutionEnums;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandExecutor.Execution
{
  /// <summary>
  /// Основной исполнитель команд контроля.
  /// </summary>
  public class CommandExecutionManager
  {
    /// <summary>
    /// Объект синхронизации ссылки на активный исполнитель.
    /// </summary>
    private static readonly object ActiveManagerSync = new();

    /// <summary>
    /// Активный исполнитель программы контроля.
    /// </summary>
    private static CommandExecutionManager? _activeManager;

    /// <summary>
    /// Объект синхронизации текущей и выбранной команд.
    /// </summary>
    private readonly object _commandStateSync = new();

    /// <summary>
    /// Ограничитель одновременных запросов выбора команды.
    /// </summary>
    private readonly SemaphoreSlim _commandJumpSemaphore = new(1, 1);

    /// <summary>
    /// Текущая команда программы контроля.
    /// </summary>
    private BaseCommandModel? _currentCommand;

    /// <summary>
    /// Команда, выбранная для отложенного перехода.
    /// </summary>
    private BaseCommandModel? _pendingJumpCommand;

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
    /// Ошибки текущей попытки команды, публикуемые только после принятия результата оператором.
    /// </summary>
    private List<ErrorItem>? _attemptErrors;
    private readonly object _attemptErrorsSync = new();

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
    public void AddErrorMethod(ErrorItem errorItem)
    {
      lock (_attemptErrorsSync)
      {
        if (_attemptErrors != null)
        {
          _attemptErrors.Add(errorItem);
          return;
        }
      }

      AddError?.Invoke(errorItem);
    }

    public List<BaseCommandModel> CommandsToExecute { get; set; } = new();

    public IReadOnlyList<BaseCommandModel> GetCommandsSnapshot() => _commands.Snapshot();

    /// <summary>
    /// Открывает выбор команды для активного приостановленного исполнителя.
    /// </summary>
    /// <returns>Задача, представляющая обработку запроса перехода.</returns>
    public static Task RequestPausedCommandJumpAsync()
    {
      CommandExecutionManager? manager;
      lock (ActiveManagerSync)
      {
        manager = _activeManager;
      }

      return manager?.RequestCommandJumpAsync() ?? Task.CompletedTask;
    }

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
      lock (ActiveManagerSync)
      {
        _activeManager = this;
      }

      try
      {
        await ExecuteAllCoreAsync().ConfigureAwait(false);
      }
      finally
      {
        lock (_commandStateSync)
        {
          _currentCommand = null;
          _pendingJumpCommand = null;
        }

        lock (ActiveManagerSync)
        {
          if (ReferenceEquals(_activeManager, this))
          {
            _activeManager = null;
          }
        }
      }
    }

    private async Task ExecuteAllCoreAsync()
    {
      int index = 0;
      CommandExecutionState.LastCuResult = null;
      CommandExecutionState.LastRejectFlag = false;

      while (index < _commands.Count)
      {
        var command = _commands[index];
        SetCurrentCommand(command);
        try
        {
          await _console.WaitIfPausedAsync();

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

          int? jumpToIndex = null;
          bool hasExecutor = _executorRegistry.TryGet(command.Mnemonic, out var executor);

          bool hasExecutionErrors;
          while (true)
          {
            var protocolSnapshot = ProtocolModelSnapshot.Capture(_protocolModel);
            lock (_attemptErrorsSync)
            {
              _attemptErrors = new List<ErrorItem>();
            }

            var context = new CommandExecutionContext(
              this, command, _console, _textEditor, _opkFilePath);
            context.JumpToCommandNumber = targetLabel =>
            {
              jumpToIndex = ResolveJumpIndex(targetLabel);
            };

            using (ControlProgramCommandExecutionContext.Enter())
            {
              if (hasExecutor)
              {
                await executor.ExecuteAsync(context, _protocolModel);
                await _console.WaitIfPausedAsync();
              }
              else
              {
                await ExecutionMessages.PublishUnknownCommandAsync(command.Mnemonic, _console);
              }
            }

            int executionErrorCount = GetNewExecutionErrorCount(command, protocolSnapshot);
            hasExecutionErrors = !hasExecutor || executionErrorCount > 0;
            var action = hasExecutionErrors
              ? await _console.ConfirmControlProgramCommandRetryAsync(
                hasExecutor ? executionErrorCount : 1)
              : UserAction.None;

            if (action == UserAction.Retry)
            {
              protocolSnapshot.Restore(_protocolModel);
              lock (_attemptErrorsSync)
              {
                _attemptErrors = null;
              }
              jumpToIndex = null;
              continue;
            }

            if (action == UserAction.Abort)
            {
              _protocolModel.CompletionStatus = ExecutionCompletionStatus.Interrupted;
              throw new OperationCanceledException(
                "Выполнение завершено оператором.",
                _console.GetCancellationToken());              
            }

            FlushAttemptErrors();
            await _console.CompleteCommandAsync(hasExecutionErrors);
            break;
          }

          if (command is not UpCommandModel and not CuCommandModel)
          {
            CommandExecutionState.LastRejectFlag = hasExecutionErrors;
          }

          if (jumpToIndex.HasValue)
          {
            index = jumpToIndex.Value;
            continue;
          }
        }
        catch (CommandJumpRequestedException)
        {
          lock (_attemptErrorsSync)
          {
            _attemptErrors = null;
          }
          var targetCommand = TakePendingJumpCommand();
          if (targetCommand == null)
          {
            throw;
          }

          await CommandJumpService.PrepareAsync(targetCommand, _console).ConfigureAwait(false);
          var targetIndex = _commands.IndexOf(targetCommand);
          if (targetIndex < 0)
          {
            throw new InvalidOperationException("Выбранная команда отсутствует в выполняемой программе контроля.");
          }

          index = targetIndex;
          SetCurrentCommand(targetCommand);
          if (targetCommand.FormattedStartLineNumber >= 0)
          {
            _textEditor.SetActiveLine(targetCommand.FormattedStartLineNumber);
          }

          continue;
        }
        //catch (OperationCanceledException)
        //{
        //  _protocolModel.CompletionStatus =
        //      ExecutionCompletionStatus.Interrupted;
        //}
        catch (Exception ex)
        {
          FlushAttemptErrors();
          await _console.CompleteCommandAsync(true);
          await ExecuteKscOnExceptionAsync(command, ex);
          throw;
        }

        index++;
      }
    }

    private async Task RequestCommandJumpAsync()
    {
      if (_console is not IExecutionCommandJumpGate { IsExecutionPaused: true } commandJumpGate)
      {
        return;
      }

      await _commandJumpSemaphore.WaitAsync().ConfigureAwait(false);
      try
      {
        BaseCommandModel? currentCommand;
        lock (_commandStateSync)
        {
          currentCommand = _currentCommand;
        }

        if (currentCommand == null || !commandJumpGate.IsExecutionPaused)
        {
          return;
        }

        var selectedCommand = await CommandJumpService.SelectAsync(
          currentCommand,
          _commands.Snapshot(),
          _console.GetCancellationToken()).ConfigureAwait(false);

        if (selectedCommand == null || !commandJumpGate.IsExecutionPaused)
        {
          return;
        }

        lock (_commandStateSync)
        {
          _pendingJumpCommand = selectedCommand;
        }

        commandJumpGate.InterruptPauseForCommandJump();
      }
      finally
      {
        _commandJumpSemaphore.Release();
      }
    }

    private void SetCurrentCommand(BaseCommandModel command)
    {
      lock (_commandStateSync)
      {
        _currentCommand = command;
      }
    }

    private BaseCommandModel? TakePendingJumpCommand()
    {
      lock (_commandStateSync)
      {
        var command = _pendingJumpCommand;
        _pendingJumpCommand = null;
        return command;
      }
    }

    private int? ResolveJumpIndex(string targetLabel)
    {
      if (string.IsNullOrWhiteSpace(targetLabel))
      {
        return null;
      }

      if (!int.TryParse(targetLabel, out var targetNumber))
      {
        return null;
      }

      var targetCommand = _commands.FindByNumber(targetNumber);
      if (targetCommand == null)
      {
        return null;
      }

      var targetIndex = _commands.IndexOf(targetCommand);
      return targetIndex >= 0 ? targetIndex : null;
    }

    private int GetNewExecutionErrorCount(
      BaseCommandModel command,
      ProtocolModelSnapshot snapshot)
    {
      var commandPrefix = $"{command.CommandNumber} ";
      int currentCount = CountCommandErrors(_protocolModel.Errors, commandPrefix);
      int previousCount = CountCommandErrors(snapshot.Errors, commandPrefix);
      return Math.Max(0, currentCount - previousCount);
    }

    private static int CountCommandErrors(
      Dictionary<string, List<ShowMessageModel>> errors,
      string commandPrefix) =>
      errors
        .Where(kvp => kvp.Key.StartsWith(commandPrefix, StringComparison.OrdinalIgnoreCase))
        .Sum(kvp => kvp.Value.Count);

    private void FlushAttemptErrors()
    {
      List<ErrorItem>? errors;
      lock (_attemptErrorsSync)
      {
        errors = _attemptErrors;
        _attemptErrors = null;
      }

      if (errors == null)
      {
        return;
      }

      foreach (var error in errors)
      {
        AddError?.Invoke(error);
      }
    }

    private sealed class ProtocolModelSnapshot
    {
      public Dictionary<string, List<ShowMessageModel>> Errors { get; }
      private Dictionary<string, List<ShowMessageModel>> Info { get; }

      private ProtocolModelSnapshot(
        Dictionary<string, List<ShowMessageModel>> errors,
        Dictionary<string, List<ShowMessageModel>> info)
      {
        Errors = errors;
        Info = info;
      }

      public static ProtocolModelSnapshot Capture(ProtocolModel model) =>
        new(Clone(model.Errors), Clone(model.Info));

      public void Restore(ProtocolModel model)
      {
        model.Errors = Clone(Errors);
        model.Info = Clone(Info);
      }

      private static Dictionary<string, List<ShowMessageModel>> Clone(
        Dictionary<string, List<ShowMessageModel>> source) =>
        source.ToDictionary(
          pair => pair.Key,
          pair => new List<ShowMessageModel>(pair.Value),
          source.Comparer);
    }

    public BaseCommandModel? GetNextCommand(BaseCommandModel currentCommand)
    {
      var index = _commands.IndexOf(currentCommand);
      if (index < 0 || index + 1 >= _commands.Count)
      {
        return null;
      }

      return _commands[index + 1];
    }

    private async Task ExecuteKscOnExceptionAsync(BaseCommandModel failedCommand, Exception ex)
    {
      if (_isExecutingEmergencyKsc)
        return;

      using var finalizationScope = EquipmentExecutionContext.EnterMandatoryFinalization();
      _isExecutingEmergencyKsc = true;
      StepControlManager.EnableStepMode(false);
      try
      {
        if (!IsStepCancellation(ex))
        {
          await ExecutionMessages.PublishEmergencyExecutionAsync(
            $"{failedCommand.CommandNumber} {failedCommand.Mnemonic}",
            ex.Message,
            _console);
        }

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
        await ExecutionMessages.PublishEmergencyKscErrorAsync(kscEx.Message, _console);
      }
      finally
      {
        _isExecutingEmergencyKsc = false;
      }
    }

    private static bool IsStepCancellation(Exception ex)
    {
      if (ex is OperationCanceledException)
        return true;

      if (ex.InnerException is OperationCanceledException)
        return true;

      // иногда кидают просто Exception с текстом
      if (ex.Message.Contains("Ожидание пошаговой команды было прервано"))
        return true;

      return false;
    }
  }
}
