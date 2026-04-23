using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Core.Shared.Metadata.Static;
using Message;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Ask.Core.Shared.DTO.Protocol.ShowMessageModel;
using static Ask.Core.Shared.Metadata.Static.DelegateManager;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Controls.ProtocolNew
{
  /// <inheritdoc />
  public partial class ProtocolUI : IUserInteractionService, IMessageOutputService, IExecutionController, IInputFieldProvider, IDeviceSelectorProvider
  {
    #region Поля.

    /// <summary>
    /// Последнее отображенное сообщение в протоколе.
    /// </summary>
    private ShowMessageModel LastModelMeassage;

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

    private bool _checkPower = true;

    private TaskCompletionSource<UserAction> _userActionTcs;

    public ErrorManager Errors;

    #region Делегаты выполнения.

    /// <summary>
    /// Делегат, вызываемый для начала действия.
    /// </summary>
    private StartDelegate _startDelegate;

    /// <summary>
    /// Делегат, вызываемый для остановки действия.
    /// </summary>
    private StopDelegate _stopDelegate;

    /// <summary>
    /// Делегат, вызываемый для возврата к предыдущему состоянию.
    /// </summary>
    private ReturnDelegate _returnDelegate;

    private PreActionDelegate _preActionDelegate;

    private bool _isRepeatEnabled;
    private string? _lastSavedProtocolPath;
    #endregion

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
    public void SetSettings(
      StartDelegate StartDelegate,
      bool isRepeatEnabled,
      StopDelegate StopDelegate = null,
      ReturnDelegate ReturnDelegate = null,
      PreActionDelegate preActionDelegate = null,
      bool checkPower = true)
    {
      Errors = new ErrorManager(ErrorListBoxVertical);
      try
      {
        _stopDelegate = StopDelegate;
        _startDelegate = StartDelegate;
        _returnDelegate = ReturnDelegate;
        _preActionDelegate = preActionDelegate;
        _checkPower = checkPower;

        if (ReturnDelegate != null)
        {
          _isRepeatEnabled = true;
        }
      }
      catch (Exception ex)
      {
        LogException("Ошибка загрузки элемента", ex);
        throw;
      }
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
    public async Task AbortExecution() => await ActionExecutor.StopAsync(_stopDelegate, _userActionTcs);

    /// <summary>
    /// Начинает запуск измерения.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию измерения.</returns>
    public async Task StartAsync() => await ActionExecutor.StartAsync(_startDelegate, _stopDelegate, header.Text, _isRepeatEnabled, _preActionDelegate, _checkPower);

    /// <summary>
    /// Завершение текущей выполняемой задачи.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию завершения.</returns>
    private async Task StopAsync() => await ActionExecutor.StopAsync(_stopDelegate, _userActionTcs);

    /// <summary>
    /// Выполняет завершающие действия после завершения процесса.
    /// </summary>
    /// <param name="stopDelegate">Делегат завершения процесса (необязательно).</param>
    /// <returns>Задача, представляющая асинхронную операцию завершения.</returns>
    public async Task FinalizeAsync(StopDelegate stopDelegate = null) => await ActionExecutor.FinalizeAsync(stopDelegate);

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
    private async void LoopMeasureEvent() => await ActionExecutor.LoopMeasureEvent(_returnDelegate, _stopDelegate);

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
    public async Task ShowMessageAsync(ShowMessageModel showMessageModel, bool IsBlockStart = false, bool SkipStepModeCheck = false, bool skipPause = false,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0)
    {
      await CheckBlockStart(IsBlockStart);

      if (ProtocolConfig.GetTimeStart() && showMessageModel.Status != MessageType.Info && showMessageModel.Status != MessageType.Command)
      {
        showMessageModel.Time = SystemStateManager._stopwatch.Elapsed.ToString(@"mm\:ss\.fff", CultureInfo.InvariantCulture);
      }

      if (AdminConfig.GetDebugRights())
      {
        if (string.IsNullOrEmpty(showMessageModel.Debug))
        {
          showMessageModel.Debug = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})";
        }
        else
        {
          showMessageModel.Debug += $"|| {Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})";
        }
      }

      await ShouldShowDetailedProtocol(showMessageModel);
      await CheckStatus(showMessageModel);

      if (string.IsNullOrEmpty(showMessageModel.Message) &&
          showMessageModel.Status != MessageType.Command &&
          !ProtocolConfig.GetHeaderInfo())
      {
        return;
      }

      await protocolTextBox.AppendLineAsync(showMessageModel, LastMessage);
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
      await MoveToLineAsync(item.SourceLineNumber);
    }


    /// <summary>
    /// Проверяет статус сообщения и добавляет текстовую приставку и цвет, если статус не является информационным.
    /// </summary>
    /// <param name="showMessageModel">Модель отображаемого сообщения, передаётся по ссылке.</param>
    private async Task CheckStatus(ShowMessageModel showMessageModel)
    {
      if (showMessageModel.Status != MessageType.Info)
      {
        if (string.IsNullOrEmpty(showMessageModel.Message))
        {
          showMessageModel.Message += showMessageModel.GetQualityPrefix();
        }
        else
        {
          var prefix = showMessageModel.GetQualityPrefix();
          if (!showMessageModel.Message.Contains(prefix))
            showMessageModel.Message += " " + prefix;
        }
        showMessageModel.MessageColor = showMessageModel.GetColorMessage();
      }

      await CheckSyntaxHighlighting(showMessageModel);
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
    /// Проверяет, нужно ли отображать детализированный протокол.
    /// Если не нужно, удаляет последнее сообщение, если оно допускает удаление и не содержит ошибки выполнения.
    /// </summary>
    /// <param name="showMessageModel">Модель текущего сообщения, которое потенциально будет сохранено как последнее.</param>
    private async Task ShouldShowDetailedProtocol(ShowMessageModel showMessageModel)
    {
      if (!ProtocolConfig.GetShowDetailedProtocol())
      {
        if (LastModelMeassage != null && LastModelMeassage.CanBeDeleted && !LastModelMeassage.ExecutionError)
        {
          await protocolTextBox.RemoveLastLinesAsync();
        }

        LastModelMeassage = showMessageModel;
      }
    }

    private async Task CheckSyntaxHighlighting(ShowMessageModel showMessageModel)
    {
      if (!UserInterfaceConfig.GetSyntaxHighlighting())
      {
        showMessageModel.HeaderColor = (Color)Application.Current.Resources["tests.protocol.message.header.foreground"];
        showMessageModel.MessageColor = (Color)Application.Current.Resources["tests.protocol.message.header.foreground"];
        showMessageModel.TimeColor = (Color)Application.Current.Resources["tests.protocol.message.header.foreground"];
        showMessageModel.HeaderBackgroundColor = null;
        return;
      }
    }

    /// <summary>
    /// Полностью очищает протокол и сбрасывает последнее сообщение.
    /// </summary>
    /// <returns>Возвращает признак успешного завершения операции.</returns>
    public async Task<bool> ClearAllMessagesAsync()
    {
      await protocolTextBox.ClearAsync();
      LastModelMeassage = null;

      if (ActionExecutor.IsPaused)
      {
        await ActionExecutor.WaitWhilePausedAsync(GetCancellationToken(), this);
      }

      Errors?.ErrorClear();
      return ActionExecutor.StepMode;
    }

    /// <summary>
    /// Асинхронно удаляет блок, содержащий указанную строку, из RichTextBox.
    /// </summary>
    /// <param name="textToRemove">Строка для поиска и удаления.</param>
    /// <returns>True, если блок был найден и удален; иначе False.</returns>
    public async Task<bool> RemoveLineContainingTextAsync(string textToRemove) => await protocolTextBox.RemoveLineContainingTextAsync(textToRemove);

    /// <summary>
    /// Сохраняет протокол в файл с автоматически сгенерированным именем в фоновом режиме асинхронно.
    /// </summary>
    public async Task SaveProtocolAsync(string name, string extention)
    {
      string filename = BuildDerivedFileName(name, extention);
      string datePath = $"{DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture)}";
      string fullPath = Path.Combine($"..\\{FileLocations.DataSaveDirectory}", $"{datePath}", filename);
      if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
      {
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
      }

      var lines = protocolTextBox.GetMessagesSnapshot()
        .Select(FormatProtocolLineForSave)
        .Where(static line => !string.IsNullOrWhiteSpace(line));

      await File.WriteAllLinesAsync(fullPath, lines);
      _lastSavedProtocolPath = Path.GetFullPath(fullPath);
    }

    private static string BuildDerivedFileName(string? sourceName, string extension)
    {
      string baseName = Path.GetFileNameWithoutExtension(sourceName);
      if (string.IsNullOrWhiteSpace(baseName))
      {
        baseName = "protocol";
      }

      return $"{baseName}{extension}";
    }

    private static string FormatProtocolLineForSave(ShowMessageModel message)
    {
      string header = message.Header?.TrimEnd() ?? string.Empty;
      string body = message.Message?.TrimEnd() ?? string.Empty;

      bool hasHeader = !string.IsNullOrWhiteSpace(header);
      bool hasBody = !string.IsNullOrWhiteSpace(body);

      if (!hasHeader && !hasBody)
      {
        return string.Empty;
      }

      if (!hasHeader)
      {
        return body;
      }

      if (!hasBody)
      {
        return header;
      }

      string separator = header.EndsWith(' ') || body.StartsWith(' ') ? string.Empty : " ";
      return $"{header}{separator}{body}";
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

