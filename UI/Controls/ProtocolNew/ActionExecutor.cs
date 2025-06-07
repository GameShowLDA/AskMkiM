using System.Windows;
using System.Windows.Input;
using NewCore.Communication;
using Utilities.Models;
using WindowsInput;
using static AppConfiguration.Base.EventAggregator;
using static AppConfiguration.Execution.ExecutionConfig;
using static AppConfiguration.Protocol.ProtocolConfig;
using static AppConfiguration.SystemState.SystemStateManager;
using static Utilities.DelegateManager;
using static Utilities.LoggerUtility;
using static Utilities.Models.ShowMessageModel;

namespace UI.Controls.ProtocolNew
{
  /// <summary>
  /// Класс, отвечающий за выполнение процессов самоконтроля и управления процессами системы.
  /// Обеспечивает запуск, остановку, паузу и пошаговый режим выполнения задач.
  /// </summary>
  public class ActionExecutor
  {
    static public event Action<bool> StartProcessing;

    bool isExit = false;

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
    ProtocolUI ProtocolSelfCheck;

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
    internal TaskCompletionSource<bool> PauseCompletionSource { get; set; }

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

      await ProtocolSelfCheck.ClearAllMessagesAsync();
      if (!await GetIsIdleModeEnabled() && !await GetIsActivePower() && checkPower)
      {
          await ProtocolSelfCheck.ShowMessageAsync(new ShowMessageModel("Нет подключения к системе. Пожалуйста, подключитесь к системе и повторите попытку.", type: MessageType.Error));
          await FinalizeAsync();
          return;
      }

      if (preActionDelegate != null)
      {
        preActionDelegate(ProtocolSelfCheck.GetCancellationToken());
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


      if (await GetIsStepByStepModeEnabled())
      {
        StepControlManager.EnableStepMode(true);
        StepMode = true;
      }

      if (IsProcessRunning(name))
      {
        return;
      }

      await PrepareForStartAsync(name);

      if (!await GetIsIdleModeEnabled())
      {
        await ResetSystemAsync();
      }

      await ExecuteTaskAsync(startDelegate, stop, name, isRepeatEnabled);
    }


    /// <summary>
    /// Завершение текущей выполняемой задачи.
    /// </summary>
    /// <param name="stopDelegate">Делегат для завершения задачи.</param>
    /// <returns>Задача, представляющая асинхронную операцию завершения процесса.</returns>
    internal async Task StopAsync(StopDelegate stopDelegate)
    {

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
        return;

      isExit = true;


      LogInformation($"Завершение \"{name}\"");

      SerialPortHelper.CloseAllRegisteredSerialPorts();

      await CancelProcessTaskAsync(stopDelegate, name);

      ResetState();

      await ResetSystemAsync();

      await HandleProtocolActionsAsync();

      ProtocolSelfCheck.ShowOnlyStartButton();

      await DisplayCompletionMessage();

      StartProcessing?.Invoke(false);
    }

    /// <summary>
    /// Ставит выполнение метода на паузу.
    /// </summary>
    internal async Task PauseAsync()
    {
      if (!IsPaused)
      {
        LogInformation("Срабатывание паузы при самоконтроле");
        IsPaused = true;
        PauseCompletionSource = new TaskCompletionSource<bool>();
        ProtocolSelfCheck.ShowButtonsOnPause();
      }

      await WaitWhilePausedAsync();
    }

    /// <summary>
    /// Возобновляет выполнение метода после паузы.
    /// </summary>
    /// <param name="stepMode">Флаг, указывающий, нужно ли возобновить в пошаговом режиме.</param>
    internal void Resume(bool stepMode)
    {
      LogInformation("Срабатывание возобновления при самоконтроле");
      if (IsPaused && PauseCompletionSource != null && !PauseCompletionSource.Task.IsCompleted)
      {
        PauseCompletionSource.SetResult(true);
      }

      IsPaused = false;
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
    internal async Task ReturnMeasureEvent(ReturnDelegate returnDelegate, StopDelegate stop)
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
        MessageBox.Show($"Ошибка токена отмены: {ex.Message}", $"Ошибка CancellationTokenSource", MessageBoxButton.OK, MessageBoxImage.Error);
        await FinalizeAsync(stop);
      }
      catch (Exception ex)
      {
        LogException("Системная ошибка", ex);
        MessageBox.Show($"Системная ошибка : {ex}! \r\rПожалуйста, обратитесь к администратору", $"Ошибка CancellationTokenSource", MessageBoxButton.OK, MessageBoxImage.Error);
        await FinalizeAsync(stop);
      }
    }

    #endregion

    #region Дополнительные методы управления.

    /// <summary>
    /// Ожидает, пока выполнение процесса находится в состоянии паузы.
    /// </summary>
    /// <param name="protocolSelfCheck">Объект интерфейса.</param>
    /// <returns>Задача ожидания паузы.</returns>
    public async Task WaitWhilePausedAsync(ProtocolUI protocolSelfCheck = null)
    {
      if (IsPaused && PauseCompletionSource != null && !PauseCompletionSource.Task.IsCompleted)
      {
        LogInformation("Срабатывание ожидания при самоконтроле");
        if (protocolSelfCheck != null)
        {
          if (ShouldShowPauseMessage)
          {
            ShouldShowPauseMessage = false;
            ShouldShowResumeMessage = true;

            ShowMessageModel showMessage = new ShowMessageModel()
            {
              Header = "Выполнение поставлено на паузу!",
              CanBeDeleted = false,
            };

            await protocolSelfCheck.ShowMessageAsync(showMessage);
          }
        }

        await PauseCompletionSource.Task;
        ShouldShowPauseMessage = true;
      }

      if (protocolSelfCheck != null)
      {
        if (ShouldShowResumeMessage)
        {
          ShouldShowResumeMessage = false;

          ShowMessageModel showMessage = new ShowMessageModel()
          {
            Header = "Выполнение снято с паузы!",
            CanBeDeleted = false,
          };
          await protocolSelfCheck.ShowMessageAsync(showMessage);
        }
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
        await WaitWhilePausedAsync().ConfigureAwait(true);
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
    /// Подготавливает систему к запуску нового процесса.
    /// </summary>
    /// <param name="name">Имя запускаемого процесса.</param>
    private async Task PrepareForStartAsync(string name)
    {
      LogInformation($"Запуск \"{name}\"");

      if (await GetTimeStart())
      {
        _stopwatch.Restart();
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
      PauseCompletionSource = new TaskCompletionSource<bool>();

      if (startDelegate != null)
      {
        try
        {
          _stopwatch.Restart();

          // Запускаем задачу с новым токеном
          ProcessTask = Task.Run(() => startDelegate(CancellationTokenSource.Token));
          await SetIsLocked(true);
          await ProcessTask;

          // После выполнения задачи
          if (isRepeatEnabled)
          {
            ProtocolSelfCheck.ShowAdditionalFunctionButtons();
          }
          else
          {
            await ProtocolSelfCheck.FinalizeAsync(stop);
          }
        }
        catch (Exception ex)
        {
          LogException($"Ошибка при запуске \"{name}\"", ex);
          await SetIsLocked(false);
          _stopwatch.Stop();
          await ProtocolSelfCheck.FinalizeAsync(stop);
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
      if (ProcessTask != null && !ProcessTask.IsCompleted)
      {
        try
        {
          // Отмена токена
          CancellationTokenSource?.Cancel();
          LogInformation($"Процесс \"{name}\" запрошен на завершение.");
        }
        catch (Exception ex)
        {
          LogException($"Ошибка при завершении \"{name}\"", ex);
        }

        try
        {
          // Ждём завершения задачи
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

      if (stopDelegate != null)
      {
        var token = CancellationTokenSource?.Token ?? CancellationToken.None;

        StepControlManager.DisableStepMode();
        KeyboardManager.TriggerStep();

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
        if (!await GetIsIdleModeEnabled())
        {
          await NewCore.Communication.DeviceCommandSender.ResetAllSystem();
        }

        await SetIsLocked(false);

        if (await GetTimeStart())
        {
          _stopwatch.Stop();
        }

        RaiseInfoMessage("");
      });
    }

    /// <summary>
    /// Обрабатывает действия, связанные с протоколом, такие как сохранение и печать.
    /// </summary>
    private async Task HandleProtocolActionsAsync()
    {
      if (await GetSaveProtocol())
      {
        await ProtocolSelfCheck.SaveProtocolAsync();
      }

      //if (await GetPrintProtocol())
      //{
      //  ProtocolSelfCheck.PrintProtocol();
      //}

      await SetIsLocked(false);
    }
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
      actionExecutor.StepMode = await GetIsStepByStepModeEnabled();
      actionExecutor.IsPaused = false;
      actionExecutor.ProcessTask = null;
      return actionExecutor;
    }
    #endregion
  }
}
