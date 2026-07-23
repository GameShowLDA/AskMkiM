using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Exceptions;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.UI.Controls.ProtocolNew;
using Ask.UI.Features.ProtocolNew.Hotkeys;
using Ask.UI.Features.ProtocolNew.Protocol;
using Ask.UI.Features.ProtocolNew.Services;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WindowsInput;
using static Ask.Core.Shared.DTO.Protocol.ShowMessageModel;
using static Ask.Core.Shared.Metadata.Static.DelegateManager;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Features.ProtocolNew.Execution
{
  /// <summary>
  /// Класс, отвечающий за выполнение процессов самоконтроля и управления процессами системы.
  /// Обеспечивает запуск, остановку, паузу и пошаговый режим выполнения задач.
  /// </summary>
  public class ActionExecutor
  {
    /// <summary>
    /// Выполняет последовательное исполнение управляющих команд
    /// с поддержкой запуска, паузы и пошагового режима.
    /// </summary>
    public ActionExecutor()
    {
      _runGuard = new ExecutionRunGuard();
      _pauseController = new ExecutionPauseController();
      _systemResetService = new ExecutionSystemResetService();
      var protocolCompletionService = new ProtocolCompletionService(new InspectionProtocolBuilder());
      _finalizer = new ExecutionFinalizer(_systemResetService, protocolCompletionService);
      EventAggregator.Subscribe<ExecutionEvents.StepByStepModeChanged>(e => StepMode = e.IsEnabled);
    }

    /// <summary>
    /// Возникает при изменении состояния выполнения процесса.
    /// </summary>
    static public event Action<bool> StartProcessing;

    /// <summary>
    /// Признак необходимости завершения выполнения процесса.
    /// </summary>
    private bool isExit = false;

    /// <summary>
    /// Имя текущего выполняемого процесса.
    /// </summary>
    private string processName = string.Empty;

    private ActionSettings? _actionSettings;

    /// <summary>
    /// Объект синхронизации для операций паузы и возобновления выполнения.
    /// </summary>
    private readonly IExecutionRunGuard _runGuard;

    /// <summary>
    /// Глобальный объект синхронизации, предотвращающий одновременный запуск
    /// нескольких экземпляров исполнителя.
    /// </summary>
    private readonly ExecutionPauseController _pauseController;

    /// <summary>
    /// Признак запроса перехода к другой команде.
    /// </summary>
    private int _commandJumpRequested;

    /// <summary>
    /// Идентификатор последнего запроса паузы.
    /// </summary>
    private long _pauseRequestId;

    /// <summary>
    /// Метка времени последнего запроса паузы.
    /// </summary>
    private long _pauseRequestedTimestamp;

    /// <summary>
    /// Признак регистрации фактического достижения паузы.
    /// </summary>
    private int _pauseReachedLogged;

    /// <summary>
    /// Признак регистрации выхода исполнителя из паузы.
    /// </summary>
    private int _pauseReleasedLogged;

    /// <summary>
    /// Ссылка на текущий активный экземпляр исполнителя.
    /// </summary>
    private readonly IExecutionSystemResetService _systemResetService;

    /// <summary>
    /// Координатор завершающей последовательности выполнения.
    /// </summary>
    private readonly ExecutionFinalizer _finalizer;

    /// <summary>
    /// Ресурсы и задача текущего запуска.
    /// </summary>
    private ExecutionSession? _session;

    /// <summary>
    /// Объект синхронизации коллекции ошибок текущего запуска.
    /// </summary>
    private readonly object _errorSync = new();

    #region Проверка токена.

    /// <summary>
    /// Источник токена отмены для управления выполняемыми задачами.
    /// </summary>
    internal CancellationTokenSource? CancellationTokenSource => _session?.Cancellation;

    #endregion

    #region Свойства.

    /// <summary>
    /// Экземпляр подключаемого класса.
    /// </summary>
    private ProtocolUI ProtocolSelfCheck;

    /// <summary>
    /// Флаг, указывающий, находится ли выполнение в состоянии паузы.
    /// </summary>
    public bool IsPaused => _pauseController.IsPaused;

    /// <summary>
    /// Флаг, указывающий, нужно ли показывать сообщение о паузе при входе в состояние паузы.
    /// </summary>
    internal bool ShouldShowPauseMessage { get; set; }

    /// <summary>
    /// Флаг, указывающий, нужно ли показывать сообщение о снятии с паузы при выходе из состояния паузы.
    /// </summary>
    internal bool ShouldShowResumeMessage { get; set; }

    /// <summary>
    /// Источник завершения задачи для управления паузой.
    /// </summary>
    internal Task? ProcessTask
    {
      get => _session?.ProcessTask;
      set
      {
        if (_session != null)
        {
          _session.ProcessTask = value;
        }
      }
    }

    /// <summary>
    /// Флаг, указывающий, находится ли выполнение в пошаговом режиме.
    /// </summary>
    internal bool StepMode
    {
      get;
      set;
    }

    #endregion

    #region Методы выполнения.

    /// <summary>
    /// Запуск самоконтроля/режима.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию запуска процесса.</returns>
    internal async Task StartAsync(ActionSettings actionSettings)
    {
      isExit = false;
      processName = actionSettings.Name;
      Interlocked.Exchange(ref _commandJumpRequested, 0);
      _pauseController.Reset();

      if (!_runGuard.TryAcquire(actionSettings.Name, this, out var activeProcessName))
      {
        LogWarning($"Попытка запустить \"{actionSettings.Name}\", пока выполняется \"{activeProcessName}\".");
        await ProtocolSelfCheck.ShowMessageAsync(new ShowMessageModel($"Уже выполняется \"{activeProcessName}\". Дождитесь завершения текущей задачи.", type: MessageType.Error), skipPause: true);
        return;
      }

      try
      {
        _actionSettings = actionSettings;
        ClearErrors();
        ProtocolSelfCheck.HideProtocolManager();
        ProtocolSelfCheck.ClearInspectionProtocol();

        // Новый запуск не должен наследовать "залипшее" состояние
        // брейкпоинта/пошагового режима от предыдущего выполнения.
        StepControlManager.Reset();
        if (StepControlManager.StepMode)
        {
          StepControlManager.DisableStepMode();
        }
        StepMode = false;

        await ProtocolSelfCheck.ClearAllMessagesAsync();
        if (!ExecutionConfig.GetIsIdleModeEnabled() && !SystemStateManager.GetIsActivePower() && actionSettings.CheckPower)
        {
          await ProtocolSelfCheck.ShowMessageAsync(new ShowMessageModel("Нет связи с системой. Пожалуйста, подключитесь к системе и повторите попытку.", type: MessageType.Error), skipPause: true);
          await FinalizeAsync(actionSettings);
          return;
        }

        if (actionSettings.PreActionDelegate != null)
        {
          await actionSettings.PreActionDelegate(ProtocolSelfCheck.GetCancellationToken());
        }

        if (actionSettings.StartDelegate == null)
        {
          await ProtocolSelfCheck.ShowMessageAsync(new ShowMessageModel("Системная ошибка выполнения, обратитесь к администратору", type: MessageType.Error));
          await FinalizeAsync(actionSettings);
          LogError("Системная ошибка выполнения, обратитесь к администратору");
          return;
        }

        ProtocolSelfCheck.ShowOnlyStopAndFinishButtons();
        StartProcessing?.Invoke(true);

        if (ExecutionConfig.GetIsStepByStepModeEnabled())
        {
          StepControlManager.EnableStepMode(true);
          StepMode = true;
        }

        if (IsProcessRunning(actionSettings.Name))
        {
          return;
        }

        PrepareForStartAsync(actionSettings.Name);

        if (!ExecutionConfig.GetIsIdleModeEnabled())
        {
          await _systemResetService.ResetAsync();
        }

        await ExecuteTaskAsync(actionSettings);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка при запуске \"{actionSettings.Name}\"", ex);
        await ProtocolSelfCheck.ShowMessageAsync(new ShowMessageModel("Системная ошибка запуска. Проверьте журнал и повторите попытку.", type: MessageType.Error), skipPause: true);
        try
        {
          await FinalizeAsync(actionSettings);
        }
        catch (Exception finalizeEx)
        {
          LogException($"Ошибка при аварийном завершении \"{actionSettings.Name}\"", finalizeEx);
          _runGuard.Release(this);
          SystemStateManager.SetIsLocked(false);
        }
      }
    }

    /// <summary>
    /// Завершение текущей выполняемой задачи.
    /// </summary>
    /// <param name="stopDelegate">Делегат для завершения задачи.</param>
    /// <returns>Задача, представляющая асинхронную операцию завершения процесса.</returns>
    internal async Task StopAsync(ActionSettings actionSettings, TaskCompletionSource<UserAction> _userActionTcs)
    {
      _userActionTcs?.TrySetResult(UserAction.Abort);
      await FinalizeAsync(actionSettings);
    }

    /// <summary>
    /// Выполняет завершающие действия после завершения самоконтроля или режима.
    /// </summary>
    /// <param name="stopDelegate">Делегат для завершения задачи (по умолчанию null).</param>
    /// <param name="name">Имя завершаемого процесса (по умолчанию null).</param>
    /// <returns>Задача, представляющая асинхронную операцию завершения.</returns>
    internal async Task FinalizeAsync(ActionSettings actionSettings)
    {
      if (isExit)
      {
        return;
      }

      isExit = true;
      LogInformation($"Завершение \"{actionSettings.Name}\"");

      await _finalizer.FinalizeAsync(
        actionSettings,
        ProtocolSelfCheck,
        () => CancelProcessTaskAsync(actionSettings.StopDelegate, actionSettings.Name),
        ResetState,
        value => StartProcessing?.Invoke(value));
    }

    /// <summary>
    /// Ставит выполнение метода на паузу.
    /// </summary>
    internal async Task PauseAsync(CancellationToken cancellationToken, IUserInteractionService userMessageService)
    {
      if (RequestPause())
      {
        LogInformation("Срабатывание паузы при самоконтроле");
        ProtocolSelfCheck.ShowButtonsOnPause();
      }

      await WaitWhilePausedAsync(cancellationToken);
    }

    /// <summary>
    /// Регистрирует запрос на паузу и подготавливает ожидание продолжения.
    /// Возвращает <c>true</c>, если пауза была запрошена впервые.
    /// </summary>
    internal bool RequestPause()
    {
      var requested = _pauseController.RequestPause();
      if (!requested)
      {
        LogInformation(
          $"[PauseTiming] Pause request ignored (already paused): executor={RuntimeHelpers.GetHashCode(this)}, " +
          $"requestId={Volatile.Read(ref _pauseRequestId)}, thread={Environment.CurrentManagedThreadId}");
        return false;
      }

      var requestId = Interlocked.Increment(ref _pauseRequestId);
      Volatile.Write(ref _pauseRequestedTimestamp, Stopwatch.GetTimestamp());
      Volatile.Write(ref _pauseReachedLogged, 0);
      Volatile.Write(ref _pauseReleasedLogged, 0);

      LogInformation(
        $"[PauseTiming] Pause requested: executor={RuntimeHelpers.GetHashCode(this)}, " +
        $"requestId={requestId}, thread={Environment.CurrentManagedThreadId}, utc={DateTime.UtcNow:O}");
      return true;
    }

    /// <summary>
    /// Прерывает ожидание паузы для перехода к другой команде.
    /// </summary>
    internal void InterruptPauseForCommandJump()
    {
      if (!IsPaused)
      {
        return;
      }

      Interlocked.Exchange(ref _commandJumpRequested, 1);
      _pauseController.InterruptWait();
    }

    /// <summary>
    /// Возобновляет выполнение метода после паузы.
    /// </summary>
    /// <param name="stepMode">Флаг, указывающий, нужно ли возобновить в пошаговом режиме.</param>
    internal void Resume(bool stepMode, IUserInteractionService userMessageService, TaskCompletionSource<UserAction> _userActionTcs)
    {
      LogInformation("Срабатывание возобновления при самоконтроле");

      var requestId = Volatile.Read(ref _pauseRequestId);
      var requestedTimestamp = Volatile.Read(ref _pauseRequestedTimestamp);
      var elapsed = requestedTimestamp == 0
        ? TimeSpan.Zero
        : Stopwatch.GetElapsedTime(requestedTimestamp);
      LogInformation(
        $"[PauseTiming] Resume requested: executor={RuntimeHelpers.GetHashCode(this)}, " +
        $"requestId={requestId}, elapsedSincePauseRequestMs={elapsed.TotalMilliseconds:F1}, " +
        $"thread={Environment.CurrentManagedThreadId}");

      _pauseController.Resume();

      _userActionTcs?.TrySetResult(UserAction.Continue);
    }

    /// <summary>
    /// Обработчик события нажатия на кнопку нижнего слоя.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события.</param>
    internal void StepIn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      var inputSimulator = new InputSimulator();
      inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.F11);
    }

    /// <summary>
    /// Обработчик события нажатия на кнопку верхнего слоя.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события.</param>
    public void StepAround_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      var inputSimulator = new InputSimulator();
      inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.F10);
    }

    /// <summary>
    /// Выполняет повтор действия, зарегистрированного в <see cref="IUserInteractionService"/>, при нажатии на кнопку "Повторить".
    /// Если повторное действие не задано, ничего не происходит.
    /// </summary>
    /// <returns>Задача, представляющая выполнение действия повтора.</returns>
    internal async Task ReturnMeasureEvent(IUserInteractionService _userMessageService, TaskCompletionSource<UserAction> _userActionTcs)
    {
      _userActionTcs?.TrySetResult(UserAction.Retry);
    }

    #endregion

    #region Дополнительные методы управления.

    /// <summary>
    /// Ожидает, пока выполнение процесса находится в состоянии паузы.
    /// </summary>
    /// <param name="protocolSelfCheck">Объект интерфейса.</param>
    /// <returns>Задача ожидания паузы.</returns>
    /// <summary>
    /// Ожидает, пока выполнение процесса находится в состоянии паузы.
    /// Поддерживает отмену ожидания через CancellationToken.
    /// </summary>
    /// <param name="protocolSelfCheck">Объект интерфейса для вывода сообщений.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Задача ожидания выхода из паузы или отмены.</returns>
    public async Task WaitWhilePausedAsync(CancellationToken cancellationToken, IMessageOutputService protocolSelfCheck = null)
    {
      if (IsPaused)
      {
        LogInformation("Срабатывание ожидания при самоконтроле");

        if (protocolSelfCheck != null && ShouldShowPauseMessage)
        {
          ShouldShowPauseMessage = false;
          ShouldShowResumeMessage = true;

          ShowMessageModel showMessage = new ShowMessageModel
          {
            Header = "Выполнение поставлено на паузу!",
            CanBeDeleted = false,
          };

          await protocolSelfCheck.ShowMessageAsync(showMessage);
        }

        try
        {
          await _pauseController.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
          LogInformation("Ожидание паузы прервано по отмене");
          return;
        }
        finally
        {
          ShouldShowPauseMessage = true;
        }

        if (IsPaused)
        {
          return;
        }
      }

      if (protocolSelfCheck != null && ShouldShowResumeMessage)
      {
        ShouldShowResumeMessage = false;

        ShowMessageModel showMessage = new ShowMessageModel
        {
          Header = "Выполнение снято с паузы!",
          CanBeDeleted = false,
        };

        await protocolSelfCheck.ShowMessageAsync(showMessage);
      }
    }

    /// <summary>
    /// Ожидает продолжения выполнения в указанной контрольной точке.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены ожидания.</param>
    /// <param name="protocolSelfCheck">Сервис вывода сообщений протокола.</param>
    /// <param name="checkpoint">Имя контрольной точки выполнения.</param>
    /// <returns>Задача, представляющая ожидание продолжения выполнения.</returns>
    /// <exception cref="OperationCanceledException">
    /// Выбрасывается, если запрошена отмена через <paramref name="cancellationToken"/>.
    /// </exception>
    internal async Task WaitAtExecutionCheckpointAsync(
      CancellationToken cancellationToken,
      IMessageOutputService protocolSelfCheck,
      string checkpoint)
    {
      ThrowIfCommandJumpRequested();

      if (IsPaused)
      {
        var requestId = Volatile.Read(ref _pauseRequestId);
        var requestedTimestamp = Volatile.Read(ref _pauseRequestedTimestamp);
        if (Interlocked.CompareExchange(ref _pauseReachedLogged, 1, 0) == 0)
        {
          var elapsed = requestedTimestamp == 0
            ? TimeSpan.Zero
            : Stopwatch.GetElapsedTime(requestedTimestamp);
          LogInformation(
            $"[PauseTiming] Execution pause reached: executor={RuntimeHelpers.GetHashCode(this)}, " +
            $"requestId={requestId}, checkpoint={checkpoint}, latencyMs={elapsed.TotalMilliseconds:F1}, " +
            $"thread={Environment.CurrentManagedThreadId}, utc={DateTime.UtcNow:O}");
        }

        await WaitWhilePausedAsync(cancellationToken, protocolSelfCheck).ConfigureAwait(false);

        ThrowIfCommandJumpRequested();

        if (Interlocked.CompareExchange(ref _pauseReleasedLogged, 1, 0) == 0)
        {
          var releaseReason = cancellationToken.IsCancellationRequested
            ? "Cancellation"
            : "Resume";
          LogInformation(
            $"[PauseTiming] Execution pause released: executor={RuntimeHelpers.GetHashCode(this)}, " +
            $"requestId={requestId}, checkpoint={checkpoint}, reason={releaseReason}, " +
            $"thread={Environment.CurrentManagedThreadId}, " +
            $"utc={DateTime.UtcNow:O}");
        }

        return;
      }

      await WaitWhilePausedAsync(cancellationToken, protocolSelfCheck).ConfigureAwait(false);
      ThrowIfCommandJumpRequested();
    }

    /// <summary>
    /// Прерывает текущую команду, если запрошен переход к другой команде.
    /// </summary>
    private void ThrowIfCommandJumpRequested()
    {
      if (Interlocked.Exchange(ref _commandJumpRequested, 0) == 1)
      {
        ShouldShowPauseMessage = false;
        ShouldShowResumeMessage = true;
        throw new CommandJumpRequestedException();
      }
    }

    /// <summary>
    /// Ожидает заданное время активного выполнения без учёта времени на паузе.
    /// </summary>
    /// <param name="delay">Продолжительность активного ожидания.</param>
    /// <param name="cancellationToken">Токен отмены ожидания.</param>
    /// <returns>Задача, представляющая ожидание указанного интервала.</returns>
    /// <exception cref="OperationCanceledException">
    /// Выбрасывается, если запрошена отмена через <paramref name="cancellationToken"/>.
    /// </exception>
    public async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
      if (delay <= TimeSpan.Zero)
      {
        await WaitAtExecutionCheckpointAsync(
          cancellationToken,
          ProtocolSelfCheck,
          "PauseAwareDelay").ConfigureAwait(false);
        return;
      }

      var remaining = delay;
      while (remaining > TimeSpan.Zero)
      {
        await WaitAtExecutionCheckpointAsync(
          cancellationToken,
          ProtocolSelfCheck,
          "PauseAwareDelay").ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var stopwatch = Stopwatch.StartNew();
        using var raceCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var delayTask = Task.Delay(remaining, raceCancellation.Token);
        var pauseTask = _pauseController.WaitForPauseRequestAsync(raceCancellation.Token);
        var completedTask = await Task.WhenAny(delayTask, pauseTask).ConfigureAwait(false);
        stopwatch.Stop();
        await raceCancellation.CancelAsync().ConfigureAwait(false);

        if (completedTask == delayTask)
        {
          await delayTask.ConfigureAwait(false);
          return;
        }

        await pauseTask.ConfigureAwait(false);
        remaining -= stopwatch.Elapsed;
      }
    }

    /// <summary>
    /// Проверка на паузу или завершение программы.
    /// </summary>
    /// <param name="token">Токен отмены.</param>
    /// <returns>True, если программа должна продолжить выполнение; false, если программа должна завершиться.</returns>
    public async Task<bool> CheckStatusProgram(CancellationToken token)
    {
      if (token.IsCancellationRequested)
      {
        return false;
      }

      if (IsPaused)
      {
        await WaitAtExecutionCheckpointAsync(
          token,
          ProtocolSelfCheck,
          "CheckStatusProgram").ConfigureAwait(true);
      }

      return true;
    }

    /// <summary>
    /// Проверяет, выполняется ли уже процесс.
    /// </summary>
    /// <param name="name">Имя запускаемого процесса.</param>
    /// <returns>True, если процесс уже выполняется; иначе false.</returns>
    private bool IsProcessRunning(string name)
    {
      if (ProcessTask != null && !ProcessTask.IsCompleted)
      {
        LogWarning($"Попытка запустить \"{name}\", когда уже выполняется другая задача.");
        return true;
      }

      return false;
    }

    /// <summary>
    /// Резервирует глобальный слот выполнения для текущего ProtocolUI.
    /// Гарантирует, что одновременно выполняется только один протокол.
    /// </summary>
    /// <summary>
    /// Добавляет текст ошибки в результаты текущего запуска.
    /// </summary>
    /// <param name="error">Текст ошибки без итогового маркера качества.</param>
    internal void AddError(string error)
    {
      if (string.IsNullOrWhiteSpace(error))
      {
        return;
      }

      lock (_errorSync)
      {
        _actionSettings?.ExecutionErrors.Add(error);
      }
    }

    /// <summary>
    /// Очищает ошибки, накопленные настройками текущего запуска.
    /// </summary>
    internal void ClearErrors()
    {
      lock (_errorSync)
      {
        _actionSettings?.ExecutionErrors.Clear();
      }
    }

    /// <summary>
    /// Подготавливает систему к запуску нового процесса.
    /// </summary>
    /// <param name="name">Имя запускаемого процесса.</param>
    private void PrepareForStartAsync(string name)
    {
      LogInformation($"Запуск \"{name}\"");

      if (ProtocolConfig.GetTimeStart())
      {
        SystemStateManager._stopwatch.Restart();
      }
    }

    /// <summary>
    /// Выполняет задачу, используя предоставленный делегат.
    /// </summary>
    /// <param name="startDelegate">Делегат для выполнения задачи.</param>
    /// <param name="name">Имя запускаемого процесса.</param>
    /// <returns>Задача, представляющая асинхронную операцию выполнения.</returns>
    private async Task ExecuteTaskAsync(ActionSettings actionSettings)
    {
      // Освобождаем ресурсы предыдущего запуска, если они ещё существуют.
      _session?.Dispose();
      isExit = false;

      // Создаём изолированное состояние нового запуска.
      _session = new ExecutionSession(actionSettings);
      _pauseController.Reset();

      if (actionSettings.StartDelegate != null)
      {
        try
        {
          SystemStateManager._stopwatch.Restart();
          actionSettings.StartTime = TimeOnly.FromDateTime(DateTime.Now);

          ProcessTask = Task.Run(() => actionSettings.StartDelegate(
            ProtocolSelfCheck,
            ProtocolSelfCheck,
            ProtocolSelfCheck.GetInputHighlightService(),
            _session.Cancellation.Token));
          SystemStateManager.SetIsLocked(true);
          await ProcessTask;
        }
        catch (OperationCanceledException)
        {
          // Отмена ожидаема при остановке выполнения.
        }
        catch (Exception ex)
        {
          LogException($"Ошибка при запуске \"{actionSettings.Name}\"", ex);
          await ProtocolSelfCheck.AppendEmptyLineAsync();
          await ProtocolSelfCheck.ShowMessageAsync(new ShowMessageModel("Системная ошибка программы АСК-МКИ-М", headerColor: ShowMessageModel.ErrorMessage.TitleColor, message: ex.Message) { IndentLevel = 1 });
        }
        finally
        {
          SystemStateManager.SetIsLocked(false);

          actionSettings.ExecutionDuration = SystemStateManager._stopwatch.Elapsed;
          SystemStateManager._stopwatch.Stop();
          await ProtocolSelfCheck.FinalizeAsync();
        }
      }
    }

    /// <summary>
    /// Отменяет текущую задачу процесса, если она выполняется.
    /// </summary>
    /// <param name="stopDelegate">Делегат для завершения задачи.</param>
    /// <param name="name">Имя завершаемого процесса.</param>
    /// <returns>Задача, представляющая асинхронную операцию отмены.</returns>
    private async Task CancelProcessTaskAsync(StopDelegate stopDelegate, string name)
    {
      _pauseController.Cancel();

      if (ProcessTask != null && !ProcessTask.IsCompleted)
      {
        try
        {
          CancellationTokenSource?.Cancel();
          LogInformation($"Процесс \"{name}\" запрошен на завершение.");
        }
        catch (Exception ex)
        {
          LogException($"Ошибка при завершении \"{name}\"", ex);
        }

        try
        {
          await ProcessTask;
        }
        catch (OperationCanceledException)
        {
          LogInformation($"Процесс \"{name}\" был отменён.");
        }
        catch (Exception ex)
        {
          LogException($"Ошибка при ожидании завершения задачи \"{name}\"", ex);
        }
      }
      else
      {
        LogWarning($"Попытка завершить \"{name}\", когда задача не запущена.");
      }

      StepControlManager.DisableStepMode();
      KeyboardManager.TriggerStep();

      if (stopDelegate != null)
      {
        try
        {
          var token = CancellationTokenSource?.Token ?? CancellationToken.None;
          await stopDelegate(token);
        }
        catch (Exception ex)
        {
          LogException($"Ошибка обязательного завершающего делегата \"{name}\".", ex);
        }
      }

      if (_session != null)
      {
        _session.ProcessTask = null;
        _session.Dispose();
        _session = null;
      }
    }

    /// <summary>
    /// Сбрасывает состояние выполнения и интерфейса.
    /// </summary>
    private void ResetState()
    {
      ProcessTask = null;
      _pauseController.Reset();
      StepMode = false;
      ShouldShowPauseMessage = true;
      ShouldShowResumeMessage = false;
      _runGuard.Release(this);

      ProtocolSelfCheck.HideExecutionButtonsAfterReset();
    }

    #endregion

    #region Настройки подключения к классу.

    /// <summary>
    /// Создает экземпляр <see cref="ActionExecutor"/>.
    /// </summary>
    /// <typeparam name="T">Тип родительского класса.</typeparam>
    /// <param name="parentClass">Экземпляр родительского класса.</param>
    /// <returns>Настроенный экземпляр <see cref="ActionExecutor"/>.</returns>
    public static ActionExecutor CreateInstance<T>(T parentClass)
    {
      try
      {
        if (parentClass != null)
        {
          if (parentClass.GetType() == typeof(ProtocolUI))
          {
            return DefaultSettings(parentClass as ProtocolUI);
          }
        }
      }
      catch (Exception)
      {
        LogError("ошибка при создании экземпляра ActionExecutor");
        return null;
      }

      return null;
    }

    /// <summary>
    /// Устанавливает настройки <see cref="ActionExecutor"/> по умолчанию.
    /// </summary>
    /// <param name="parentClass">Экземпляр <see cref="ProtocolUI"/>.</param>
    /// <returns>Настроенный экземпляр <see cref="ActionExecutor"/>.</returns>
    static private ActionExecutor DefaultSettings(ProtocolUI parentClass)
    {
      var actionExecutor = new ActionExecutor();
      actionExecutor.ProtocolSelfCheck = parentClass;
      actionExecutor.ShouldShowPauseMessage = true;
      actionExecutor.ShouldShowResumeMessage = false;
      actionExecutor.StepMode = ExecutionConfig.GetIsStepByStepModeEnabled();
      actionExecutor._pauseController.Reset();
      return actionExecutor;
    }
    #endregion
  }
}
