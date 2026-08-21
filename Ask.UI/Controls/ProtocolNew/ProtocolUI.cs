using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.UI.Features.ProtocolNew.Controls;
using Ask.UI.Features.ProtocolNew.Execution;
using Ask.UI.Features.ProtocolNew.Protocol;
using Message;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Controls.ProtocolNew
{
  /// <inheritdoc />
  public partial class ProtocolUI : IUserInteractionService, IMessageOutputService, IExecutionController, IExecutionPauseGate, IInputFieldProvider, IDeviceSelectorProvider, IProtocolEntrySink, IProtocolPostOutputContext, IInspectionProtocolAreaView, IProtocolErrorListView
  {
    #region РџРѕР»СЏ.

    /// <summary>
    /// Сервис подготовки и вывода одной записи протокола.
    /// </summary>
    private ProtocolEntryOutputService _entryOutputService = null!;

    /// <summary>
    /// Контроллер паузы и пошагового выполнения после отображения записи.
    /// </summary>
    private ProtocolPostOutputController _postOutputController = null!;

    /// <summary>
    /// Р’РѕР·РІСЂР°С‰Р°РµС‚ С‚РµРєСѓС‰РёР№ СЃС‚Р°С‚СѓСЃ РїРѕС€Р°РіРѕРІРѕРіРѕ СЂРµР¶РёРјР°.
    /// </summary>
    public bool StepMode => ActionExecutor.StepMode;

    /// <summary>
    /// Р¤Р»Р°Рі, СѓРєР°Р·С‹РІР°СЋС‰РёР№, С‡С‚Рѕ С‚РµРєСѓС‰РµРµ СЃРѕРѕР±С‰РµРЅРёРµ СЏРІР»СЏРµС‚СЃСЏ РїРѕСЃР»РµРґРЅРёРј.
    /// </summary>
    public bool LastMessage { get; set; } = false;

    public IButtonService ButtonService { get; set; }

    /// <summary>
    /// Р”РµР№СЃС‚РІРёРµ, РєРѕС‚РѕСЂРѕРµ Р±СѓРґРµС‚ РІС‹Р·РІР°РЅРѕ РїСЂРё РЅР°Р¶Р°С‚РёРё РЅР° РєРЅРѕРїРєСѓ "РџРѕРІС‚РѕСЂРёС‚СЊ".
    /// </summary>
    private Func<Task> _retryAction;

    /// <summary>
    /// Р­РєР·РµРјРїР»СЏСЂ <see cref="ActionExecutor"/>, РёСЃРїРѕР»СЊР·СѓРµРјС‹Р№ РґР»СЏ РІС‹РїРѕР»РЅРµРЅРёСЏ РґРµР№СЃС‚РІРёР№.
    /// </summary>
    private readonly ActionExecutor ActionExecutor;

    private TaskCompletionSource<UserAction> _userActionTcs;
    private bool _isRetryOrContinueInteraction;

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

    #region РћСЃРЅРѕРІРЅС‹Рµ РЅР°СЃС‚СЂРѕР№РєРё.

    /// <summary>
    /// РЈСЃС‚Р°РЅР°РІР»РёРІР°РµС‚ РѕСЃРЅРѕРІРЅС‹Рµ РЅР°СЃС‚СЂРѕР№РєРё РІС‹РїРѕР»РЅРµРЅРёСЏ РґРµР№СЃС‚РІРёР№.
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
    /// РќР°СЃС‚СЂР°РёРІР°РµС‚ СЃРѕР±С‹С‚РёСЏ РґР»СЏ СЌР»РµРјРµРЅС‚РѕРІ СѓРїСЂР°РІР»РµРЅРёСЏ.
    /// </summary>
    public void SetEventControls()
    {
      StartMeasureResistanceButtonPreviewMouseDown += async (sender, e) => await StartAsync();
      PauseButtonPreviewMouseDown += async (sender, e) => await PauseAsync();

      TopLayerButtonPreviewMouseDown += StepAround_PreviewMouseDown;
      BottomLayerButtonPreviewMouseDown += StepIn_PreviewMouseDown;

      NextButtonPreviewMouseDown += (sender, e) => Resume();
      ExitButtonPreviewMouseDown += async (sender, e) => await StopAsync();

      ReturnMeasureResistanceButtonPreviewMouseDown += (sender, e) => ReturnMeasureEvent();
    }
    #endregion

    #region РћСЃРЅРѕРІРЅС‹Рµ РјРµС‚РѕРґС‹ РєРЅРѕРїРѕРє.

    #region РќР°С‡Р°Р»Рѕ Рё РєРѕРЅРµС†.

    /// <summary>
    /// РџСЂРµСЂС‹РІР°РµС‚ РІС‹РїРѕР»РЅРµРЅРёРµ С‚РµРєСѓС‰РµРіРѕ РїСЂРѕС†РµСЃСЃР°.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию прерывания выполнения.</returns>
    public async Task AbortExecution() => await ActionExecutor.StopAsync(_modeSettings.Current, _userActionTcs);

    /// <summary>
    /// РќР°С‡РёРЅР°РµС‚ Р·Р°РїСѓСЃРє РёР·РјРµСЂРµРЅРёСЏ.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию измерения.</returns>
    public async Task StartAsync()
    {
      var actionSettings = _modeSettings.Current;
      var executionName = actionSettings.NameProvider?.Invoke();
      if (!string.IsNullOrWhiteSpace(executionName))
      {
        Header = executionName;
        actionSettings.Name = executionName;
      }

      await ActionExecutor.StartAsync(actionSettings);
    }


    /// <summary>
    /// Р—Р°РІРµСЂС€РµРЅРёРµ С‚РµРєСѓС‰РµР№ РІС‹РїРѕР»РЅСЏРµРјРѕР№ Р·Р°РґР°С‡Рё.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию завершения.</returns>
    private async Task StopAsync() => await ActionExecutor.StopAsync(_modeSettings.Current, _userActionTcs);

    /// <summary>
    /// Р’С‹РїРѕР»РЅСЏРµС‚ Р·Р°РІРµСЂС€Р°СЋС‰РёРµ РґРµР№СЃС‚РІРёСЏ РїРѕСЃР»Рµ Р·Р°РІРµСЂС€РµРЅРёСЏ РїСЂРѕС†РµСЃСЃР°.
    /// </summary>
    /// <param name="stopDelegate">Делегат завершения процесса (необязательно).</param>
    /// <returns>Задача, представляющая асинхронную операцию завершения.</returns>
    public async Task FinalizeAsync() => await ActionExecutor.FinalizeAsync(_modeSettings.Current);

    #endregion

    #region РџР°СѓР·Р° Рё РїСЂРѕРґРѕР»Р¶РёС‚СЊ.

    /// <summary>
    /// РџСЂРёРѕСЃС‚Р°РЅР°РІР»РёРІР°РµС‚ РјРµС‚РѕРґ РЅР° РїР°СѓР·Сѓ.
    /// </summary>
    /// <returns></returns>
    public async Task PauseAsync() => await ActionExecutor.PauseAsync(GetCancellationToken(), this);

    /// <summary>
    /// Р’РѕР·РѕР±РЅРѕРІР»СЏРµС‚ РјРµС‚РѕРґ РїРѕСЃР»Рµ РїР°СѓР·С‹.
    /// </summary>
    public void Resume() => ActionExecutor.Resume(ActionExecutor.StepMode, this, _userActionTcs);

    /// <inheritdoc />
    Task IExecutionPauseGate.WaitIfPausedAsync(CancellationToken cancellationToken) =>
      ActionExecutor.WaitAtExecutionCheckpointAsync(cancellationToken, this, "ExecutionPauseGate");

    /// <inheritdoc />
    Task IExecutionPauseGate.DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
      ActionExecutor.DelayAsync(delay, cancellationToken);

    #endregion

    #region Повтор.

    /// <summary>
    /// Р’С‹РїРѕР»РЅСЏРµС‚ РґРµР»РµРіР°С‚ РёР·РјРµСЂРµРЅРёСЏ РѕРґРёРЅ СЂР°Р·. Р•СЃР»Рё РґРµР»РµРіР°С‚ null, РІС‹РїРѕР»РЅСЏРµС‚СЃСЏ Р·Р°РІРµСЂС€РµРЅРёРµ.
    /// </summary>
    private async void ReturnMeasureEvent() => await ActionExecutor.ReturnMeasureEvent(this, _userActionTcs);

    #endregion

    #region РџРѕ С€Р°РіР°Рј.

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚С‡РёРє СЃРѕР±С‹С‚РёСЏ РЅР°Р¶Р°С‚РёСЏ РЅР° РєРЅРѕРїРєСѓ "РџРѕРІРµСЂС…".
    /// </summary>
    private void StepAround_PreviewMouseDown(object sender, MouseButtonEventArgs e) => ActionExecutor.StepAround_PreviewMouseDown(sender, e);

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚С‡РёРє СЃРѕР±С‹С‚РёСЏ РЅР°Р¶Р°С‚РёСЏ РЅР° РєРЅРѕРїРєСѓ "Р’РіР»СѓР±СЊ".
    /// </summary>
    private void StepIn_PreviewMouseDown(object sender, MouseButtonEventArgs e) => ActionExecutor.StepIn_PreviewMouseDown(sender, e);

    #endregion

    #endregion

    #region РњРµС‚РѕРґС‹.

    /// <summary>
    /// Р’С‹РІРѕРґРёС‚ РёРЅС„РѕСЂРјР°С†РёСЋ РІ РїСЂРѕС‚РѕРєРѕР».
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
        _modeSettings.CheckType,
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
    /// РђСЃРёРЅС…СЂРѕРЅРЅРѕ РґРѕР±Р°РІР»СЏРµС‚ РїСѓСЃС‚СѓСЋ СЃС‚СЂРѕРєСѓ РІ РїСЂРѕС‚РѕРєРѕР» СЃ Р·Р°РґР°РЅРЅС‹Рј СѓСЂРѕРІРЅРµРј РѕС‚СЃС‚СѓРїР°.
    /// </summary>
    /// <param name="indentLevel">РЈСЂРѕРІРµРЅСЊ РѕС‚СЃС‚СѓРїР° (РЅРµ РёСЃРїРѕР»СЊР·СѓРµС‚СЃСЏ РІ С‚РµРєСѓС‰РµР№ СЂРµР°Р»РёР·Р°С†РёРё).</param>
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
    /// РџСЂРѕРІРµСЂСЏРµС‚, РЅРµРѕР±С…РѕРґРёРјРѕ Р»Рё РЅР°С‡Р°С‚СЊ РЅРѕРІС‹Р№ Р±Р»РѕРє. Р•СЃР»Рё РґР° вЂ” Р·Р°РІРµСЂС€Р°РµС‚ РїСЂРµРґС‹РґСѓС‰РёР№ Рё РЅР°С‡РёРЅР°РµС‚ РЅРѕРІС‹Р№.
    /// </summary>
    /// <param name="IsBlockStart">РџСЂРёР·РЅР°Рє РЅР°С‡Р°Р»Р° РЅРѕРІРѕРіРѕ Р±Р»РѕРєР°.</param>
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
    /// РџРѕР»РЅРѕСЃС‚СЊСЋ РѕС‡РёС‰Р°РµС‚ РїСЂРѕС‚РѕРєРѕР» Рё СЃР±СЂР°СЃС‹РІР°РµС‚ РїРѕСЃР»РµРґРЅРµРµ СЃРѕРѕР±С‰РµРЅРёРµ.
    /// </summary>
    /// <returns>Р’РѕР·РІСЂР°С‰Р°РµС‚ РїСЂРёР·РЅР°Рє СѓСЃРїРµС€РЅРѕРіРѕ Р·Р°РІРµСЂС€РµРЅРёСЏ РѕРїРµСЂР°С†РёРё.</returns>
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
    /// РђСЃРёРЅС…СЂРѕРЅРЅРѕ СѓРґР°Р»СЏРµС‚ Р±Р»РѕРє, СЃРѕРґРµСЂР¶Р°С‰РёР№ СѓРєР°Р·Р°РЅРЅСѓСЋ СЃС‚СЂРѕРєСѓ, РёР· RichTextBox.
    /// </summary>
    /// <param name="textToRemove">РЎС‚СЂРѕРєР° РґР»СЏ РїРѕРёСЃРєР° Рё СѓРґР°Р»РµРЅРёСЏ.</param>
    /// <returns>True, РµСЃР»Рё Р±Р»РѕРє Р±С‹Р» РЅР°Р№РґРµРЅ Рё СѓРґР°Р»РµРЅ; РёРЅР°С‡Рµ False.</returns>
    public async Task<bool> RemoveLineContainingTextAsync(string textToRemove) => await protocolTextBox.RemoveLineContainingTextAsync(textToRemove);

    /// <summary>
    /// РЎРѕС…СЂР°РЅСЏРµС‚ РїСЂРѕС‚РѕРєРѕР» РІ С„Р°Р№Р» СЃ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРё СЃРіРµРЅРµСЂРёСЂРѕРІР°РЅРЅС‹Рј РёРјРµРЅРµРј РІ С„РѕРЅРѕРІРѕРј СЂРµР¶РёРјРµ Р°СЃРёРЅС…СЂРѕРЅРЅРѕ.
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
      UpdateInspectionProtocolTitle();
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
      InspectionProtocolManager.Visibility = visibility;
    }

    /// <summary>
    /// Сохраняет итоговый протокол в каталоге истории.
    /// </summary>
    /// <param name="name">Имя сохраняемого протокола.</param>
    /// <param name="checkType">Тип завершённой проверки.</param>
    public async Task SaveInspectionProtocolAsync(string name, CheckType checkType)
    {
      await _protocolStorage.SaveInspectionProtocolAsync(name, checkType);
    }

    #endregion

    /// <summary>
    /// Р’РѕР·РІСЂР°С‰Р°РµС‚ С‚РѕРєРµРЅ РѕС‚РјРµРЅС‹ РґР»СЏ С‚РµРєСѓС‰РµРіРѕ РґРµР№СЃС‚РІРёСЏ, РµСЃР»Рё РёСЃС‚РѕС‡РЅРёРє РЅРµ СѓРЅРёС‡С‚РѕР¶РµРЅ.
    /// </summary>
    /// <returns>РўРѕРєРµРЅ РѕС‚РјРµРЅС‹ <see cref="CancellationToken"/> РёР»Рё <see cref="CancellationToken.None"/>.</returns>
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
      MessageBoxCustom.Show("Р’ Р±СѓРґСѓС‰РµРј РґРѕР±Р°РІРёС‚СЊ СЃСЋРґР° СЂРµР°Р»РёР·Р°С†РёСЋ РІС‹Р±РѕСЂР°", image: MessageBoxImage.Error);
      return Task.FromResult(true);
    }

    /// <summary>
    /// РђСЃРёРЅС…СЂРѕРЅРЅРѕ РѕР¶РёРґР°РµС‚ РґРµР№СЃС‚РІРёРµ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ РїРѕСЃР»Рµ РІРѕР·РЅРёРєРЅРѕРІРµРЅРёСЏ РѕС€РёР±РєРё РёР»Рё РѕСЃС‚Р°РЅРѕРІРєРё.
    /// </summary>
    /// <remarks>
    /// Аппаратная операция ожидает решения независимо от настройки остановки при ошибке.
    /// Для отрицательного результата измерения ожидание определяется этой настройкой.
    /// </remarks>
    /// <param name="loop">Признак обязательного интерактивного режима.</param>
    /// <param name="deviceTask">Признак аппаратной операции.</param>
    /// <param name="canContinue">Признак доступности продолжения после последней попытки.</param>
    /// <returns>
    /// Выбранное действие или <see cref="UserAction.None"/>, если ожидание не требуется.
    /// </returns>
    public async Task<UserAction> WaitUserActionAsync(
      bool loop = false,
      bool deviceTask = false,
      bool canContinue = true)
    {
      bool stopOnError = await ExecutionConfig.GetIsStopOnErrorEnabled();
      if (ShouldWaitForUserAction(stopOnError, loop, deviceTask))
      {
        _userActionTcs = new TaskCompletionSource<UserAction>(
          TaskCreationOptions.RunContinuationsAsynchronously);
        SetNonVisibleAllButton();
        ShowInteractiveActionButtons(canContinue);

        return await _userActionTcs.Task;
      }

      return UserAction.None;
    }

    /// <inheritdoc />
    public async Task<UserAction> WaitRetryOrContinueAsync()
    {
      _userActionTcs = new TaskCompletionSource<UserAction>(
        TaskCreationOptions.RunContinuationsAsynchronously);
      _isRetryOrContinueInteraction = true;
      SetNonVisibleAllButton();
      _buttonController.Apply(ProtocolButtonState.RetryOrContinue);

      try
      {
        return await _userActionTcs.Task;
      }
      finally
      {
        _isRetryOrContinueInteraction = false;
      }
    }

    /// <summary>
    /// Определяет необходимость ожидания решения оператора.
    /// </summary>
    /// <param name="stopOnError">Настройка остановки при отрицательном результате измерения.</param>
    /// <param name="loop">Признак обязательного интерактивного режима.</param>
    /// <param name="deviceTask">Признак аппаратной операции.</param>
    /// <returns>
    /// <see langword="true"/>, если требуется ожидать решение оператора.
    /// В противном случае — <see langword="false"/>.
    /// </returns>
    internal static bool ShouldWaitForUserAction(
      bool stopOnError,
      bool loop,
      bool deviceTask)
    {
      return stopOnError || loop || deviceTask;
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

    /// <inheritdoc />
    public string GetExecutionTitle()
    {
      if (Dispatcher.CheckAccess())
        return Header;

      return Dispatcher.Invoke(() => Header);
    }

    /// <inheritdoc />
    public void SetExecutionInputParameters(IReadOnlyList<string> parameters)
    {
      void SetParameters()
      {
        _modeSettings.Current.InputParameters.Clear();
        _modeSettings.Current.InputParameters.AddRange(parameters);
      }

      if (Dispatcher.CheckAccess())
        SetParameters();
      else
        Dispatcher.Invoke(SetParameters);
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
