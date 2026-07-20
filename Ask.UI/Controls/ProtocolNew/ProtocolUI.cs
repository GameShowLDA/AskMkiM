using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.UI.Features.ProtocolNew.Execution;
using Ask.UI.Features.ProtocolNew.Errors;
using Ask.UI.Features.ProtocolNew.Protocol;
using Message;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Ask.LogLib.LoggerUtility;
using static Ask.Core.Shared.DTO.Protocol.ShowMessageModel;

namespace Ask.UI.Controls.ProtocolNew
{
  /// <inheritdoc />
  public partial class ProtocolUI : IUserInteractionService, IMessageOutputService, IExecutionController, IExecutionPauseGate, IInputFieldProvider, IDeviceSelectorProvider, IProtocolEntrySink, IProtocolPostOutputContext, IInspectionProtocolAreaView, IProtocolErrorListView
  {
    #region Поля.

    /// <summary>
    /// Сервис подготовки и вывода одной записи протокола.
    /// </summary>
    private ProtocolEntryOutputService _entryOutputService = null!;

    /// <summary>
    /// Контроллер паузы и пошагового выполнения после отображения записи.
    /// </summary>
    private ProtocolPostOutputController _postOutputController = null!;

    /// <summary>
    /// Возвращает текущий статус пошагового режима.
    /// </summary>
    public bool StepMode => ActionExecutor.StepMode;

    /// <summary>
    /// Флаг, указывающий, что текущее сообщение является последним.
    /// </summary>
    public bool LastMessage { get; set; } = false;

    public IButtonService ButtonService { get; set; }

    /// <summary>
    /// Действие, которое будет вызвано при нажатии на кнопку "Повторить".
    /// </summary>
    private Func<Task> _retryAction;

    /// <summary>
    /// Экземпляр <see cref="ActionExecutor"/>, используемый для выполнения действий.
    /// </summary>
    private readonly ActionExecutor ActionExecutor;

    private TaskCompletionSource<UserAction> _userActionTcs;

    public ErrorManager Errors;

    /// <summary>
    /// Хранилище состояния и файлов текущей пары протоколов.
    /// </summary>
    private ProtocolStorageService _protocolStorage = null!;

    /// <summary>
    /// Контроллер встроенной и внешней областей итогового протокола.
    /// </summary>
    private InspectionProtocolAreaController _inspectionProtocolAreaController = null!;

    /// <summary>
    /// Внешний владелец представления итогового протокола.
    /// Если не задан, используется встроенная панель <see cref="ProtocolUI"/>.
    /// </summary>
    public IInspectionProtocolHost? InspectionProtocolHost { get; set; }

    /// <summary>
    /// Владелец настроек текущего режима выполнения.
    /// </summary>
    private readonly ExecutionModeSettings _modeSettings = new();

    #endregion

    #region Основные настройки.

    /// <summary>
    /// Устанавливает основные настройки выполнения действий.
    /// </summary>
    /// <param name="MainWindow">Главное окно приложения.</param>
    /// <param name="StartDelegate">Делегат запуска.</param>
    /// <param name="isRepeatEnabled">Флаг разрешения повторного выполнения.</param>
    /// <param name="StopDelegate">Делегат остановки (необязательно).</param>
    /// <param name="ReturnDelegate">Делегат возврата к предыдущему состоянию (необязательно).</param>
    /// <param name="preActionDelegate">Делегат предварительных действий перед запуском (необязательно).</param>
    public void SetSettings(ActionSettings actionSettings)
    {
      Errors = new ErrorManager(this);
      _modeSettings.Configure(actionSettings, header.Text);
    }

    public void AddError(string error)
    {
      ActionExecutor.AddError(error);
    }

    public void ClearErrors()
    {
      ActionExecutor.ClearErrors();
      _modeSettings.ClearExecutionErrors();
    }

    /// <summary>
    /// Настраивает события для элементов управления.
    /// </summary>
    public void SetEventControls()
    {
      StartMeasureResistanceButtonPreviewMouseDown += async (sender, e) => await StartAsync();
      PauseButtonPreviewMouseDown += async (sender, e) => await PauseAsync();

      TopLayerButtonPreviewMouseDown += StepAround_PreviewMouseDown;
      BottomLayerButtonPreviewMouseDown += StepIn_PreviewMouseDown;

      NextButtonPreviewMouseDown += (sender, e) => Resume();
      ExitButtonPreviewMouseDown += async (sender, e) => await StopAsync();

      LoopMeasureResistanceButtonPreviewMouseDown += (sender, e) => LoopMeasureEvent();
      ReturnMeasureResistanceButtonPreviewMouseDown += (sender, e) => ReturnMeasureEvent();
    }
    #endregion

    #region Основные методы кнопок.

    #region Начало и конец.

    /// <summary>
    /// Прерывает выполнение текущего процесса.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию прерывания выполнения.</returns>
    public async Task AbortExecution() => await ActionExecutor.StopAsync(_modeSettings.Current, _userActionTcs);

    /// <summary>
    /// Начинает запуск измерения.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию измерения.</returns>
    public async Task StartAsync() => await ActionExecutor.StartAsync(_modeSettings.Current);

    /// <summary>
    /// Завершение текущей выполняемой задачи.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию завершения.</returns>
    private async Task StopAsync() => await ActionExecutor.StopAsync(_modeSettings.Current, _userActionTcs);

    /// <summary>
    /// Выполняет завершающие действия после завершения процесса.
    /// </summary>
    /// <param name="stopDelegate">Делегат завершения процесса (необязательно).</param>
    /// <returns>Задача, представляющая асинхронную операцию завершения.</returns>
    public async Task FinalizeAsync() => await ActionExecutor.FinalizeAsync(_modeSettings.Current);

    #endregion

    #region Пауза и продолжить.

    /// <summary>
    /// Приостанавливает метод на паузу.
    /// </summary>
    /// <returns></returns>
    public async Task PauseAsync() => await ActionExecutor.PauseAsync(GetCancellationToken(), this);

    /// <summary>
    /// Возобновляет метод после паузы.
    /// </summary>
    public void Resume() => ActionExecutor.Resume(ActionExecutor.StepMode, this, _userActionTcs);

    /// <inheritdoc />
    Task IExecutionPauseGate.WaitIfPausedAsync(CancellationToken cancellationToken) =>
      ActionExecutor.WaitAtExecutionCheckpointAsync(cancellationToken, this, "ExecutionPauseGate");

    /// <inheritdoc />
    Task IExecutionPauseGate.DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
      ActionExecutor.DelayAsync(delay, cancellationToken);

    #endregion

    #region Повтор и зацикливание.

    /// <summary>
    /// Запускает цикл выполнения делегата измерения, отображая кнопки "Остановить" и "Завершить".
    /// </summary>
    private async void LoopMeasureEvent() => await ActionExecutor.LoopMeasureEvent(_modeSettings.Current);

    /// <summary>
    /// Выполняет делегат измерения один раз. Если делегат null, выполняется завершение.
    /// </summary>
    private async void ReturnMeasureEvent() => await ActionExecutor.ReturnMeasureEvent(this, _userActionTcs);

    #endregion

    #region По шагам.

    /// <summary>
    /// Обработчик события нажатия на кнопку "Поверх".
    /// </summary>
    private void StepAround_PreviewMouseDown(object sender, MouseButtonEventArgs e) => ActionExecutor.StepAround_PreviewMouseDown(sender, e);

    /// <summary>
    /// Обработчик события нажатия на кнопку "Вглубь".
    /// </summary>
    private void StepIn_PreviewMouseDown(object sender, MouseButtonEventArgs e) => ActionExecutor.StepIn_PreviewMouseDown(sender, e);

    #endregion

    #endregion

    #region Методы.

    /// <summary>
    /// Выводит информацию в протокол.
    /// </summary>
    /// <param name="showMessageModel">Модель сообщения.</param>
    /// <returns>Возвращает режим по шагам.</returns>
    public async Task ShowMessageAsync(ShowMessageModel showMessageModel, bool IsBlockStart = false, bool SkipStepModeCheck = false, bool skipPause = false, bool ignoreOutputValidation = false,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0)
    {
      var outputStarted = Stopwatch.GetTimestamp();
      var messageId = RuntimeHelpers.GetHashCode(showMessageModel);

      await CheckBlockStart(IsBlockStart);
      var wasDisplayed = await _entryOutputService.WriteAsync(
        showMessageModel,
        LastMessage,
        ignoreOutputValidation,
        _modeSettings.AccumulateErrorMessages,
        AddError,
        callerName,
        callerFile,
        callerLine);

      if (!wasDisplayed)
      {
        return;
      }

      var displayedAt = Stopwatch.GetTimestamp();
      LastMessage = false;
      await _postOutputController.ProcessAsync(
        showMessageModel,
        IsBlockStart,
        SkipStepModeCheck,
        skipPause);

      var completedAt = Stopwatch.GetTimestamp();
      LogDebug(
        $"[ProtocolOutputTiming] Output completed: message={messageId}, " +
        $"dispatcherAndWriteMs={Stopwatch.GetElapsedTime(outputStarted, displayedAt).TotalMilliseconds:F1}, " +
        $"postOutputMs={Stopwatch.GetElapsedTime(displayedAt, completedAt).TotalMilliseconds:F1}, " +
        $"totalMs={Stopwatch.GetElapsedTime(outputStarted, completedAt).TotalMilliseconds:F1}, " +
        $"thread={Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Асинхронно добавляет пустую строку в протокол с заданным уровнем отступа.
    /// </summary>
    /// <param name="indentLevel">Уровень отступа (не используется в текущей реализации).</param>
    public async Task AppendEmptyLineAsync(int indentLevel = 0)
    {
      await protocolTextBox.AppendEmptyLineAsync();
    }

    public async Task CompleteCommandAsync(bool hasErrors)
    {
      await protocolTextBox.CompleteCommandAsync(hasErrors);
    }

    public int GetLastLineNumber()
    {
      return protocolTextBox.GetLastLineNumber();
    }

    public async Task MoveToLineAsync(int lineNumber)
    {
      await protocolTextBox.MoveToLineAsync(lineNumber);
    }

    /// <summary>
    /// Проверяет, необходимо ли начать новый блок. Если да — завершает предыдущий и начинает новый.
    /// </summary>
    /// <param name="IsBlockStart">Признак начала нового блока.</param>
    private async Task CheckBlockStart(bool IsBlockStart)
    {
      if (IsBlockStart)
      {
        StepControlManager.ExitBlock();
        StepControlManager.EnterBlock();
      }
    }
    private async void ErrorListBoxVertical_ErrorItemDoubleClicked(IDisplayIssue item)
    {
      var lineNumber = item.SourceLineNumber > 0
        ? item.SourceLineNumber
        : item.FormattedLineNumber;

      if (lineNumber > 0)
      {
        await MoveToLineAsync(lineNumber);
      }
    }

    /// <summary>
    /// Полностью очищает протокол и сбрасывает последнее сообщение.
    /// </summary>
    /// <returns>Возвращает признак успешного завершения операции.</returns>
    public async Task<bool> ClearAllMessagesAsync()
    {
      await protocolTextBox.ClearAsync();
      _entryOutputService.Reset();

      if (ActionExecutor.IsPaused)
      {
        await ActionExecutor.WaitWhilePausedAsync(GetCancellationToken(), this);
      }

      Errors?.ErrorClear();
      return ActionExecutor.StepMode;
    }

    /// <inheritdoc />
    Task IProtocolEntrySink.AppendLineAsync(ShowMessageModel message, bool isLastMessage) =>
      protocolTextBox.AppendLineAsync(message, isLastMessage);

    /// <inheritdoc />
    Task IProtocolEntrySink.RemoveLastLinesAsync() => protocolTextBox.RemoveLastLinesAsync();

    /// <inheritdoc />
    bool IProtocolPostOutputContext.IsPaused => ActionExecutor.IsPaused;

    /// <inheritdoc />
    Task IProtocolPostOutputContext.WaitWhilePausedAsync(CancellationToken cancellationToken) =>
      ActionExecutor.WaitAtExecutionCheckpointAsync(cancellationToken, this, "ProtocolPostOutput");

    /// <inheritdoc />
    void IProtocolPostOutputContext.ShowPauseButtons() => ShowButtonsOnPause(repeatVisible: false);

    /// <inheritdoc />
    void IProtocolPostOutputContext.ShowRunningButtons(bool showStepButtons) =>
      ShowOnlyStopAndFinishButtons(showStepButtons);

    /// <summary>
    /// Асинхронно удаляет блок, содержащий указанную строку, из RichTextBox.
    /// </summary>
    /// <param name="textToRemove">Строка для поиска и удаления.</param>
    /// <returns>True, если блок был найден и удален; иначе False.</returns>
    public async Task<bool> RemoveLineContainingTextAsync(string textToRemove) => await protocolTextBox.RemoveLineContainingTextAsync(textToRemove);

    /// <summary>
    /// Сохраняет протокол в файл с автоматически сгенерированным именем в фоновом режиме асинхронно.
    /// </summary>
    public async Task SaveProtocolAsync(string name)
    {
      await _protocolStorage.SaveExecutionProtocolAsync(name, protocolTextBox.GetMessagesSnapshot());
    }

    /// <summary>
    /// Очищает и скрывает итоговый протокол перед новым запуском.
    /// </summary>
    public void ClearInspectionProtocol()
    {
      _inspectionProtocolAreaController.Clear(InspectionProtocolHost);
    }

    /// <summary>
    /// Показывает итоговый протокол справа от протокола выполнения.
    /// </summary>
    public void ShowInspectionProtocol(string protocolText)
    {
      _inspectionProtocolAreaController.Show(protocolText, InspectionProtocolHost);
    }

    /// <inheritdoc />
    string IInspectionProtocolAreaView.ProtocolText
    {
      get => inspectionProtocolTextBox.Text;
      set => inspectionProtocolTextBox.Text = value;
    }

    /// <inheritdoc />
    void IInspectionProtocolAreaView.SetAreaVisible(bool isVisible)
    {
      var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
      InspectionProtocolColumn.Width = isVisible
        ? new GridLength(1, GridUnitType.Star)
        : new GridLength(0);
      InspectionProtocolSplitter.Visibility = visibility;
      InspectionProtocolPanel.Visibility = visibility;
    }

    /// <summary>
    /// Сохраняет итоговый протокол в History в формате RTLST.
    /// </summary>
    public async Task SaveInspectionProtocolAsync(string name)
    {
      await _protocolStorage.SaveInspectionProtocolAsync(name);
    }

    #endregion

    /// <summary>
    /// Возвращает токен отмены для текущего действия, если источник не уничтожен.
    /// </summary>
    /// <returns>Токен отмены <see cref="CancellationToken"/> или <see cref="CancellationToken.None"/>.</returns>
    public CancellationToken GetCancellationToken()
    {
      try
      {
        return ActionExecutor?.CancellationTokenSource?.Token ?? CancellationToken.None;
      }
      catch (ObjectDisposedException)
      {
        return CancellationToken.None;
      }
    }

    public Task<bool> AwaitAdminDecisionAsync(string message)
    {
      MessageBoxCustom.Show("В будущем добавить сюда реализацию выбора", image: MessageBoxImage.Error);
      return Task.FromResult(true);
    }

    /// <summary>
    /// Асинхронно ожидает действие пользователя после возникновения ошибки или остановки.
    /// </summary>
    /// <remarks>
    /// Метод создаёт новый <see cref="TaskCompletionSource{TResult}"/> для ожидания выбора пользователя 
    /// (например, продолжить, пропустить или остановить выполнение).  
    /// Если в конфигурации установлено свойство <c>IsStopOnErrorEnabled</c>,  
    /// интерфейс переходит в режим паузы — скрываются все кнопки и отображаются кнопки управления паузой.  
    /// После выбора действия пользователем результат возвращается как значение перечисления 
    /// <see cref="IUserInteractionService.UserAction"/>.
    /// </remarks>
    /// <returns>
    /// Задача, представляющая ожидаемое действие пользователя.  
    /// Если режим остановки на ошибке отключён, возвращается <see cref="IUserInteractionService.UserAction.None"/>.
    /// </returns>
    public async Task<UserAction> WaitUserActionAsync(bool loop = false, bool deviceTask = false)
    {
      _userActionTcs = new TaskCompletionSource<UserAction>();

      if (await ExecutionConfig.GetIsStopOnErrorEnabled() || loop || deviceTask)
      {

        SetNonVisibleAllButton();
        ShowButtonsOnPause(true);

        return await _userActionTcs.Task;
      }

      return UserAction.None;
    }

    public void AddError(ErrorItem errorItem)
    {
      Errors.AddError(errorItem);
    }

    /// <inheritdoc />
    void IProtocolErrorListView.AddError(ErrorItem errorItem) => ErrorListBoxVertical.AddError(errorItem);

    /// <inheritdoc />
    void IProtocolErrorListView.ClearErrors() => ErrorListBoxVertical.ClearAll();

    public IInputFieldAccessor? GetInputFieldAccessor()
    {
      IInputFieldAccessor? result = null;

      void TryGet()
      {
        if (ContentView is IInputFieldAccessor inputField)
        {
          result = inputField;
        }
      }

      if (Dispatcher.CheckAccess())
      {
        TryGet();
      }
      else
      {
        Dispatcher.Invoke(TryGet);
      }

      return result;
    }

    public IInputHighlightService? GetInputHighlightService()
    {
      IInputHighlightService? result = null;

      void TryGet()
      {
        if (ContentView is IInputHighlightService inputField)
        {
          result = inputField;
        }
      }

      if (Dispatcher.CheckAccess())
      {
        TryGet();
      }
      else
      {
        Dispatcher.Invoke(TryGet);
      }

      return result;
    }

    public UserControl GetControl()
    {
      return this;
    }

    public IDeviceSelector GetDeviceSelector()
    {
      IDeviceSelector? result = null;

      void TryGet()
      {
        if (ContentView is IDeviceSelector inputField)
        {
          result = inputField;
        }
      }

      if (Dispatcher.CheckAccess())
      {
        TryGet();
      }
      else
      {
        Dispatcher.Invoke(TryGet);
      }

      return result!;
    }
  }
}
