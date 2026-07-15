using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Protocols;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.UI.Features.ProtocolNew.Execution;
using Ask.UI.Features.ProtocolNew.Hotkeys;
using Ask.UI.Features.ProtocolNew.Protocol;
using Message;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Ask.Core.Shared.DTO.Protocol.ShowMessageModel;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Controls.ProtocolNew
{
  /// <inheritdoc />
  public partial class ProtocolUI : IUserInteractionService, IMessageOutputService, IExecutionController, IInputFieldProvider, IDeviceSelectorProvider, IProtocolEntrySink
  {
    #region Поля.

    /// <summary>
    /// Сервис подготовки и вывода одной записи протокола.
    /// </summary>
    private ProtocolEntryOutputService _entryOutputService = null!;

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

    private string? _lastSavedProtocolPath;

    private string _inspectionProtocolText = string.Empty;

    /// <summary>
    /// Внешний владелец представления итогового протокола.
    /// Если не задан, используется встроенная панель <see cref="ProtocolUI"/>.
    /// </summary>
    public IInspectionProtocolHost? InspectionProtocolHost { get; set; }

    private ActionSettings _settings;

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
      Errors = new ErrorManager(ErrorListBoxVertical);
      try
      {
        _settings = actionSettings;
        _settings.Name = header.Text;

        if (actionSettings.ReturnDelegate != null)
        {
          _settings.IsRepeatEnabled = true;
        }
      }
      catch (Exception ex)
      {
        LogException("Ошибка загрузки элемента", ex);
        throw;
      }
    }

    public void AddError(string error)
    {
      ActionExecutor.AddError(error);
    }

    public void ClearErrors()
    {
      ActionExecutor.ClearErrors();
      _settings?.ExecutionErrors.Clear();
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
    public async Task AbortExecution() => await ActionExecutor.StopAsync(_settings, _userActionTcs);

    /// <summary>
    /// Начинает запуск измерения.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию измерения.</returns>
    public async Task StartAsync() => await ActionExecutor.StartAsync(_settings);

    /// <summary>
    /// Завершение текущей выполняемой задачи.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию завершения.</returns>
    private async Task StopAsync() => await ActionExecutor.StopAsync(_settings, _userActionTcs);

    /// <summary>
    /// Выполняет завершающие действия после завершения процесса.
    /// </summary>
    /// <param name="stopDelegate">Делегат завершения процесса (необязательно).</param>
    /// <returns>Задача, представляющая асинхронную операцию завершения.</returns>
    public async Task FinalizeAsync() => await ActionExecutor.FinalizeAsync(_settings);

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

    #endregion

    #region Повтор и зацикливание.

    /// <summary>
    /// Запускает цикл выполнения делегата измерения, отображая кнопки "Остановить" и "Завершить".
    /// </summary>
    private async void LoopMeasureEvent() => await ActionExecutor.LoopMeasureEvent(_settings);

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
      await CheckBlockStart(IsBlockStart);
      var wasDisplayed = await _entryOutputService.WriteAsync(
        showMessageModel,
        LastMessage,
        ignoreOutputValidation,
        _settings?.AccumulateErrorMessages == true,
        AddError,
        callerName,
        callerFile,
        callerLine);

      if (!wasDisplayed)
      {
        return;
      }

      LastMessage = false;

      if (ActionExecutor.IsPaused)
      {
        await ActionExecutor.WaitWhilePausedAsync(GetCancellationToken(), this);
      }

      if (!skipPause)
      {
        await CheckPause(showMessageModel.Status);
      }

      if (StepControlManager.StepMode && !SkipStepModeCheck)
      {
        if (ShouldWaitStepKey(showMessageModel, IsBlockStart))
        {
          ShowButtonsOnPause(repeatVisible: false);
          await KeyboardManager.WaitForNextStepKeyAsync(GetCancellationToken());

          bool showStepButtons = StepControlManager.IsStepInto && !StepControlManager.StepOverUntilNextControlCommand;
          ShowOnlyStopAndFinishButtons(showStepButtons);
        }
      }

      await Task.Delay(1);
    }

    private static bool ShouldWaitStepKey(ShowMessageModel showMessageModel, bool isBlockStart)
    {
      if (StepControlManager.IsStepInto)
      {
        return true;
      }

      if (!StepControlManager.StepOverUntilNextControlCommand)
      {
        return false;
      }

      if (!IsControlProgramCommandStart(showMessageModel, isBlockStart))
      {
        return false;
      }

      StepControlManager.CompleteStepOverUntilNextControlCommand();
      return true;
    }

    private static bool IsControlProgramCommandStart(ShowMessageModel showMessageModel, bool isBlockStart)
    {
      if (!isBlockStart || showMessageModel.Status != MessageType.Command)
      {
        return false;
      }

      return showMessageModel.IsControlProgramCommandHeader;
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
    /// Если статус сообщения — ошибка и включена остановка при ошибке, выполнение ставится на паузу.
    /// </summary>
    /// <param name="Status">Тип сообщения (ошибка, информация, успех).</param>
    private async Task CheckPause(ShowMessageModel.MessageType? Status)
    {
      if (Status == MessageType.Error && await ExecutionConfig.GetIsStopOnErrorEnabled())
      {
        await PauseAsync();
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
      _lastSavedProtocolPath = await ExecutionProtocolHistoryService.SaveAsync(name, protocolTextBox.GetMessagesSnapshot());
    }

    /// <summary>
    /// Очищает и скрывает итоговый протокол перед новым запуском.
    /// </summary>
    public void ClearInspectionProtocol()
    {
      _inspectionProtocolText = string.Empty;
      inspectionProtocolTextBox.Text = string.Empty;
      InspectionProtocolPanel.Visibility = Visibility.Collapsed;
      InspectionProtocolSplitter.Visibility = Visibility.Collapsed;
      InspectionProtocolColumn.Width = new GridLength(0);
      InspectionProtocolHost?.ClearInspectionProtocol();
    }

    /// <summary>
    /// Показывает итоговый протокол справа от протокола выполнения.
    /// </summary>
    public void ShowInspectionProtocol(string protocolText)
    {
      _inspectionProtocolText = protocolText ?? string.Empty;

      if (InspectionProtocolHost != null)
      {
        InspectionProtocolHost.ShowInspectionProtocol(_inspectionProtocolText);
        return;
      }

      inspectionProtocolTextBox.Text = _inspectionProtocolText;
      InspectionProtocolColumn.Width = new GridLength(1, GridUnitType.Star);
      InspectionProtocolSplitter.Visibility = Visibility.Visible;
      InspectionProtocolPanel.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Сохраняет итоговый протокол в History в формате RTLST.
    /// </summary>
    public async Task SaveInspectionProtocolAsync(string name)
    {
      if (string.IsNullOrWhiteSpace(_inspectionProtocolText))
      {
        return;
      }

      await ExecutionProtocolHistoryService.SaveInspectionAsync(
        name,
        _inspectionProtocolText,
        _lastSavedProtocolPath);
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
