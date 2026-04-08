using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Services.FilesUtility;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Device.Runtime.Ethernet.Udp.Broadcast;
using Message;
using System.Windows;
using System.Windows.Input;
using WindowsInput;
using static Ask.Core.Shared.DTO.Protocol.ShowMessageModel;
using static Ask.Core.Shared.Metadata.Static.DelegateManager;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Controls.ProtocolNew
{
  /// <summary>
  /// Класс, отвечающий за выполнение процессов самоконтроля и управления процессами системы.
  /// Обеспечивает запуск, остановку, паузу и пошаговый режим выполнения задач.
  /// </summary>
  public class ActionExecutor
  {
    public ActionExecutor()
    {
      EventAggregator.Subscribe<ExecutionEvents.StepByStepModeChanged>(e => StepMode = e.IsEnabled);
    }

    static public event Action<bool> StartProcessing;

    private bool isExit = false;

    private string processName = string.Empty;

    private readonly object _pauseSync = new();
    private static readonly object _runSync = new();
    private static ActionExecutor? _activeExecutor;

    #region Проверка токена.

    /// <summary>
    /// Источник токена отмены для управления выполняемыми задачами.
    /// </summary>
    internal CancellationTokenSource CancellationTokenSource;

    #endregion

    #region Свойства.

    /// <summary>
    /// Экземпляр подключаемого класса.
    /// </summary>
    private ProtocolUI ProtocolSelfCheck;

    /// <summary>
    /// Флаг, указывающий, находится ли выполнение в состоянии паузы.
    /// </summary>
    public bool IsPaused { get; set; }

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
    internal TaskCompletionSource<bool>? PauseCompletionSource { get; set; }

    /// <summary>
    /// Задача выполнения.
    /// </summary>
    internal Task ProcessTask { get; set; }

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
    /// <param name="startDelegate">Делегат для выполнения задачи.</param>
    /// <param name="stop">Делегат для завершения задачи.</param>
    /// <param name="name">Имя запускаемого процесса.</param>
    /// <param name="isRepeatEnabled">Флаг, указывающий, повторять ли операцию.</param>
    /// <param name="preActionDelegate">Необязательный делегат для выполнения предварительных действий.</param>
    /// <returns>Задача, представляющая асинхронную операцию запуска процесса.</returns>
    internal async Task StartAsync(StartDelegate startDelegate, StopDelegate stop, string name, bool isRepeatEnabled, PreActionDelegate preActionDelegate = null, bool checkPower = true)
    {
      isExit = false;
      processName = name;
      IsPaused = false;

      if (!TryAcquireRunSlot(out var activeProcessName))
      {
        LogWarning($"Попытка запустить \"{name}\", пока выполняется \"{activeProcessName}\".");
        await ProtocolSelfCheck.ShowMessageAsync(new ShowMessageModel($"Уже выполняется \"{activeProcessName}\". Дождитесь завершения текущей задачи.", type: MessageType.Error), skipPause: true);
        return;
      }

      try
      {
        ProtocolSelfCheck.HideProtocolManager();

        // Новый запуск не должен наследовать "залипшее" состояние
        // брейкпоинта/пошагового режима от предыдущего выполнения.
        StepControlManager.Reset();
        if (StepControlManager.StepMode)
        {
          StepControlManager.DisableStepMode();
        }
        StepMode = false;

        await ProtocolSelfCheck.ClearAllMessagesAsync();
        if (!ExecutionConfig.GetIsIdleModeEnabled() && !SystemStateManager.GetIsActivePower() && checkPower)
        {
          await ProtocolSelfCheck.ShowMessageAsync(new ShowMessageModel("Нет подключения к системе. Пожалуйста, подключитесь к системе и повторите попытку.", type: MessageType.Error), skipPause: true);
          await FinalizeAsync();
          return;
        }

        if (preActionDelegate != null)
        {
          await preActionDelegate(ProtocolSelfCheck.GetCancellationToken());
        }

        if (startDelegate == null)
        {
          await ProtocolSelfCheck.ShowMessageAsync(new ShowMessageModel("Системная ошибка выполнения, обратитесь к администратору", type: MessageType.Error));
          await FinalizeAsync();
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

        if (IsProcessRunning(name))
        {
          return;
        }

        PrepareForStartAsync(name);

        if (!ExecutionConfig.GetIsIdleModeEnabled())
        {
          await ResetSystemAsync();
        }

        await ExecuteTaskAsync(startDelegate, stop, name, isRepeatEnabled);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка при запуске \"{name}\"", ex);
        await ProtocolSelfCheck.ShowMessageAsync(new ShowMessageModel("Системная ошибка запуска. Проверьте журнал и повторите попытку.", type: MessageType.Error), skipPause: true);
        try
        {
          await FinalizeAsync(stop);
        }
        catch (Exception finalizeEx)
        {
          LogException($"Ошибка при аварийном завершении \"{name}\"", finalizeEx);
          ReleaseRunSlot();
          SystemStateManager.SetIsLocked(false);
        }
      }
    }

    /// <summary>
    /// Завершение текущей выполняемой задачи.
    /// </summary>
    /// <param name="stopDelegate">Делегат для завершения задачи.</param>
    /// <returns>Задача, представляющая асинхронную операцию завершения процесса.</returns>
    internal async Task StopAsync(StopDelegate stopDelegate, TaskCompletionSource<UserAction> _userActionTcs)
    {
      _userActionTcs?.TrySetResult(UserAction.Abort);
      await FinalizeAsync(stopDelegate);
    }

    /// <summary>
    /// Выполняет завершающие действия после завершения самоконтроля или режима.
    /// </summary>
    /// <param name="stopDelegate">Делегат для завершения задачи (по умолчанию null).</param>
    /// <param name="name">Имя завершаемого процесса (по умолчанию null).</param>
    /// <returns>Задача, представляющая асинхронную операцию завершения.</returns>
    internal async Task FinalizeAsync(StopDelegate stopDelegate = null, string name = null)
    {
      if (isExit)
      {
        return;
      }

      isExit = true;
      LogInformation($"Завершение \"{processName}\"");

      await CancelProcessTaskAsync(stopDelegate, processName);
      ResetState();
      await ResetSystemAsync();

      await HandleProtocolActionsAsync(processName);
      ProtocolSelfCheck.ShowOnlyStartButton();
      await DisplayCompletionMessage();

      StartProcessing?.Invoke(false);

      await ProtocolSelfCheck.SaveProtocolAsync(ProtocolSelfCheck.Header, ".lstw");
      ProtocolSelfCheck.ShowProtocolManager();
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
      lock (_pauseSync)
      {
        if (IsPaused && PauseCompletionSource != null && !PauseCompletionSource.Task.IsCompleted)
        {
          return false;
        }

        IsPaused = true;
        PauseCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        return true;
      }
    }

    /// <summary>
    /// Возобновляет выполнение метода после паузы.
    /// </summary>
    /// <param name="stepMode">Флаг, указывающий, нужно ли возобновить в пошаговом режиме.</param>
    internal void Resume(bool stepMode, IUserInteractionService userMessageService, TaskCompletionSource<UserAction> _userActionTcs)
    {
      LogInformation("Срабатывание возобновления при самоконтроле");

      TaskCompletionSource<bool>? pauseTcs = null;
      lock (_pauseSync)
      {
        if (IsPaused)
        {
          pauseTcs = PauseCompletionSource;
        }

        IsPaused = false;
      }

      if (pauseTcs != null && !pauseTcs.Task.IsCompleted)
      {
        pauseTcs.TrySetResult(true);
      }

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
    /// Запускает цикл выполнения делегата измерения, отображая кнопки "Остановить" и "Завершить".
    /// </summary>
    /// <param name="returnDelegate">Делегат, выполняющий операцию измерения. Если null, выполняется завершение.</param>
    /// <param name="stop">Делегат для остановки операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию цикла измерения.</returns>
    internal async Task LoopMeasureEvent(ReturnDelegate returnDelegate, StopDelegate stop)
    {
      ProtocolSelfCheck.ShowOnlyStopAndFinishButtons();
      while (!CancellationTokenSource?.IsCancellationRequested ?? true)
      {
        try
        {
          await ReturnMeasureEvent(returnDelegate, stop);
        }
        catch (Exception)
        {
          break;
        }
      }
    }

    /// <summary>
    /// Выполняет операцию измерения один раз.
    /// </summary>
    /// <param name="returnDelegate">Делегат измерения.</param>
    /// <param name="stop">Делегат остановки.</param>
    /// <returns>Задача, представляющая измерение.</returns>
    private async Task ReturnMeasureEvent(ReturnDelegate returnDelegate, StopDelegate stop)
    {
      try
      {
        var token = CancellationTokenSource?.Token ?? new CancellationToken();

        if (returnDelegate != null)
        {
          await returnDelegate(token);
        }
        else
        {
          await FinalizeAsync(stop);
        }
      }
      catch (ObjectDisposedException ex)
      {
        LogException("Token уже утилизирован", ex);
        MessageBoxCustom.Show($"Ошибка токена отмены: {ex.Message}", $"Ошибка CancellationTokenSource", MessageBoxButton.OK, MessageBoxImage.Error);
        await FinalizeAsync(stop);
      }
      catch (Exception ex)
      {
        LogException("Системная ошибка", ex);
        MessageBoxCustom.Show($"Системная ошибка : {ex}! \r\rПожалуйста, обратитесь к администратору", $"Ошибка CancellationTokenSource", MessageBoxButton.OK, MessageBoxImage.Error);
        await FinalizeAsync(stop);
      }
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
      TaskCompletionSource<bool>? pauseTcs = null;
      lock (_pauseSync)
      {
        if (IsPaused)
        {
          pauseTcs = PauseCompletionSource;
        }
      }

      if (pauseTcs != null && !pauseTcs.Task.IsCompleted)
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

        using (cancellationToken.Register(() =>
        {
          LogInformation("Ожидание паузы прервано по отмене");
          pauseTcs.TrySetCanceled(cancellationToken);
        }))
        {
          try
          {
            await pauseTcs.Task;
          }
          catch (TaskCanceledException)
          {
            // Отмена ожидания — просто выйти
            return;
          }
          finally
          {
            ShouldShowPauseMessage = true;
          }
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
        await WaitWhilePausedAsync(token).ConfigureAwait(true);
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
    private bool TryAcquireRunSlot(out string activeProcessName)
    {
      lock (_runSync)
      {
        if (_activeExecutor == null || ReferenceEquals(_activeExecutor, this))
        {
          _activeExecutor = this;
          activeProcessName = string.Empty;
          return true;
        }

        activeProcessName = string.IsNullOrWhiteSpace(_activeExecutor.processName)
          ? "другая задача"
          : _activeExecutor.processName;
        return false;
      }
    }

    /// <summary>
    /// Освобождает глобальный слот выполнения.
    /// </summary>
    private void ReleaseRunSlot()
    {
      lock (_runSync)
      {
        if (ReferenceEquals(_activeExecutor, this))
        {
          _activeExecutor = null;
        }
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
    /// Отображает сообщение о завершении.
    /// </summary>
    private async Task DisplayCompletionMessage()
    {
      ShowMessageModel showMessage = new ShowMessageModel()
      {
        Header = $"Завершено",
        CanBeDeleted = false,
      };
      ProtocolSelfCheck.LastMessage = true;
      await ProtocolSelfCheck.ShowMessageAsync(showMessage);
    }

    /// <summary>
    /// Выполняет задачу, используя предоставленный делегат.
    /// </summary>
    /// <param name="startDelegate">Делегат для выполнения задачи.</param>
    /// <param name="name">Имя запускаемого процесса.</param>
    /// <returns>Задача, представляющая асинхронную операцию выполнения.</returns>
    private async Task ExecuteTaskAsync(StartDelegate startDelegate, StopDelegate stop, string name, bool isRepeatEnabled)
    {
      // Освобождаем старый токен, если был
      CancellationTokenSource?.Dispose();
      isExit = false;

      // Создаём новый токен
      CancellationTokenSource = new CancellationTokenSource();
      PauseCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

      if (startDelegate != null)
      {
        bool shouldFinalize = !isRepeatEnabled;

        try
        {
          SystemStateManager._stopwatch.Restart();

          ProcessTask = Task.Run(() => startDelegate(ProtocolSelfCheck, ProtocolSelfCheck, ProtocolSelfCheck.GetInputHighlightService(), CancellationTokenSource.Token));
          SystemStateManager.SetIsLocked(true);
          await ProcessTask;

          if (isRepeatEnabled)
          {
            ProtocolSelfCheck.ShowAdditionalFunctionButtons();
            shouldFinalize = false;
          }
        }
        catch (OperationCanceledException)
        {
          // Отмена ожидаема при остановке выполнения.
          shouldFinalize = true;
        }
        catch (Exception ex)
        {
          LogException($"Ошибка при запуске \"{name}\"", ex);
          await ProtocolSelfCheck.AppendEmptyLineAsync();
          await ProtocolSelfCheck.ShowMessageAsync(new ShowMessageModel("Системная ошибка программы АСК-МКИ-М", headerColor: ShowMessageModel.ErrorMessage.TitleColor, message: ex.Message) { IndentLevel = 1 });
          shouldFinalize = true;
        }
        finally
        {
          SystemStateManager.SetIsLocked(false);
          SystemStateManager._stopwatch.Stop();

          if (shouldFinalize)
          {
            await ProtocolSelfCheck.FinalizeAsync(stop);
          }
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
      TaskCompletionSource<bool>? pauseTcs = null;
      lock (_pauseSync)
      {
        IsPaused = false;
        pauseTcs = PauseCompletionSource;
      }

      pauseTcs?.TrySetCanceled();

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
        var token = CancellationTokenSource?.Token ?? CancellationToken.None;
        await stopDelegate(token);
      }

      CancellationTokenSource?.Dispose();
      ProcessTask = null;
    }


    /// <summary>
    /// Сбрасывает состояние выполнения и интерфейса.
    /// </summary>
    private void ResetState()
    {
      ProcessTask = null;
      IsPaused = false;
      StepMode = false;
      ShouldShowPauseMessage = true;
      ShouldShowResumeMessage = false;
      PauseCompletionSource = null;
      ReleaseRunSlot();

      Application.Current.Dispatcher.Invoke(() =>
      {
        ProtocolSelfCheck.PauseButtonVisibility = Visibility.Collapsed;
        ProtocolSelfCheck.StepOverButtonVisibility = Visibility.Collapsed;
        ProtocolSelfCheck.StepIntoButtonVisibility = Visibility.Collapsed;
        ProtocolSelfCheck.NextButtonVisibility = Visibility.Collapsed;
        ProtocolSelfCheck.ExitButtonVisibility = Visibility.Collapsed;
      });
    }

    /// <summary>
    /// Сбрасывает состояние системы.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию сброса.</returns>
    private async Task ResetSystemAsync()
    {
      await Application.Current.Dispatcher.Invoke(async () =>
      {
        await UdpBroadcastCommandSender.ResetAllDevicesAsync();

        SystemStateManager.SetIsLocked(false);

        if (ProtocolConfig.GetTimeStart())
        {
          SystemStateManager._stopwatch.Stop();
        }

        MessageEventAdapter.RaiseInfoMessage("");
      });
    }

    /// <summary>
    /// Обрабатывает действия, связанные с протоколом, такие как сохранение и печать.
    /// </summary>
    private async Task HandleProtocolActionsAsync(string name)
    {
      if (await ProtocolConfig.GetSaveProtocol())
      {
        await ProtocolSelfCheck.SaveProtocolAsync(name, ".txt");
      }

      if (await ProtocolConfig.GetPrintProtocol())
      {
        PrintUtility.PrintProtocol(ProtocolSelfCheck.GetShowMessageModels());
      }

      SystemStateManager.SetIsLocked(false);
    }
    #endregion

    #region Повтор действий.

    #endregion

    #region Настройки подключения к классу.

    /// <summary>
    /// Создает экземпляр <see cref="ActionExecutor"/>.
    /// </summary>
    /// <typeparam name="T">Тип родительского класса.</typeparam>
    /// <param name="parentClass">Экземпляр родительского класса.</param>
    /// <returns>Настроенный экземпляр <see cref="ActionExecutor"/>.</returns>
    public static async Task<ActionExecutor> CreateInstanceAsync<T>(T parentClass)
    {
      try
      {
        if (parentClass != null)
        {
          if (parentClass.GetType() == typeof(ProtocolUI))
          {
            return await DefaultSettings(parentClass as ProtocolUI);
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
    static private async Task<ActionExecutor> DefaultSettings(ProtocolUI parentClass)
    {
      var actionExecutor = new ActionExecutor();
      actionExecutor.ProtocolSelfCheck = parentClass;
      actionExecutor.ShouldShowPauseMessage = true;
      actionExecutor.ShouldShowResumeMessage = false;
      actionExecutor.StepMode = ExecutionConfig.GetIsStepByStepModeEnabled();
      actionExecutor.IsPaused = false;
      actionExecutor.ProcessTask = null;
      return actionExecutor;
    }
    #endregion
  }
}

