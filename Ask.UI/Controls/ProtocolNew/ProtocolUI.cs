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
    #region РџРѕР»СЏ.

    /// <summary>
    /// РџРѕСЃР»РµРґРЅРµРµ РѕС‚РѕР±СЂР°Р¶РµРЅРЅРѕРµ СЃРѕРѕР±С‰РµРЅРёРµ РІ РїСЂРѕС‚РѕРєРѕР»Рµ.
    /// </summary>
    private ShowMessageModel LastModelMeassage;

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

    private bool _checkPower = true;

    private TaskCompletionSource<UserAction> _userActionTcs;

    public ErrorManager Errors;

    #region Р”РµР»РµРіР°С‚С‹ РІС‹РїРѕР»РЅРµРЅРёСЏ.

    /// <summary>
    /// Р”РµР»РµРіР°С‚, РІС‹Р·С‹РІР°РµРјС‹Р№ РґР»СЏ РЅР°С‡Р°Р»Р° РґРµР№СЃС‚РІРёСЏ.
    /// </summary>
    private StartDelegate _startDelegate;

    /// <summary>
    /// Р”РµР»РµРіР°С‚, РІС‹Р·С‹РІР°РµРјС‹Р№ РґР»СЏ РѕСЃС‚Р°РЅРѕРІРєРё РґРµР№СЃС‚РІРёСЏ.
    /// </summary>
    private StopDelegate _stopDelegate;

    /// <summary>
    /// Р”РµР»РµРіР°С‚, РІС‹Р·С‹РІР°РµРјС‹Р№ РґР»СЏ РІРѕР·РІСЂР°С‚Р° Рє РїСЂРµРґС‹РґСѓС‰РµРјСѓ СЃРѕСЃС‚РѕСЏРЅРёСЋ.
    /// </summary>
    private ReturnDelegate _returnDelegate;

    private PreActionDelegate _preActionDelegate;

    private bool _isRepeatEnabled;
    private string? _lastSavedProtocolPath;
    #endregion

    #endregion

    #region РћСЃРЅРѕРІРЅС‹Рµ РЅР°СЃС‚СЂРѕР№РєРё.

    /// <summary>
    /// РЈСЃС‚Р°РЅР°РІР»РёРІР°РµС‚ РѕСЃРЅРѕРІРЅС‹Рµ РЅР°СЃС‚СЂРѕР№РєРё РІС‹РїРѕР»РЅРµРЅРёСЏ РґРµР№СЃС‚РІРёР№.
    /// </summary>
    /// <param name="MainWindow">Р“Р»Р°РІРЅРѕРµ РѕРєРЅРѕ РїСЂРёР»РѕР¶РµРЅРёСЏ.</param>
    /// <param name="StartDelegate">Р”РµР»РµРіР°С‚ Р·Р°РїСѓСЃРєР°.</param>
    /// <param name="isRepeatEnabled">Р¤Р»Р°Рі СЂР°Р·СЂРµС€РµРЅРёСЏ РїРѕРІС‚РѕСЂРЅРѕРіРѕ РІС‹РїРѕР»РЅРµРЅРёСЏ.</param>
    /// <param name="StopDelegate">Р”РµР»РµРіР°С‚ РѕСЃС‚Р°РЅРѕРІРєРё (РЅРµРѕР±СЏР·Р°С‚РµР»СЊРЅРѕ).</param>
    /// <param name="ReturnDelegate">Р”РµР»РµРіР°С‚ РІРѕР·РІСЂР°С‚Р° Рє РїСЂРµРґС‹РґСѓС‰РµРјСѓ СЃРѕСЃС‚РѕСЏРЅРёСЋ (РЅРµРѕР±СЏР·Р°С‚РµР»СЊРЅРѕ).</param>
    /// <param name="preActionDelegate">Р”РµР»РµРіР°С‚ РїСЂРµРґРІР°СЂРёС‚РµР»СЊРЅС‹С… РґРµР№СЃС‚РІРёР№ РїРµСЂРµРґ Р·Р°РїСѓСЃРєРѕРј (РЅРµРѕР±СЏР·Р°С‚РµР»СЊРЅРѕ).</param>
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
        LogException("РћС€РёР±РєР° Р·Р°РіСЂСѓР·РєРё СЌР»РµРјРµРЅС‚Р°", ex);
        throw;
      }
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

      LoopMeasureResistanceButtonPreviewMouseDown += (sender, e) => LoopMeasureEvent();
      ReturnMeasureResistanceButtonPreviewMouseDown += (sender, e) => ReturnMeasureEvent();
    }
    #endregion

    #region РћСЃРЅРѕРІРЅС‹Рµ РјРµС‚РѕРґС‹ РєРЅРѕРїРѕРє.

    #region РќР°С‡Р°Р»Рѕ Рё РєРѕРЅРµС†.

    /// <summary>
    /// РџСЂРµСЂС‹РІР°РµС‚ РІС‹РїРѕР»РЅРµРЅРёРµ С‚РµРєСѓС‰РµРіРѕ РїСЂРѕС†РµСЃСЃР°.
    /// </summary>
    /// <returns>Р—Р°РґР°С‡Р°, РїСЂРµРґСЃС‚Р°РІР»СЏСЋС‰Р°СЏ Р°СЃРёРЅС…СЂРѕРЅРЅСѓСЋ РѕРїРµСЂР°С†РёСЋ РїСЂРµСЂС‹РІР°РЅРёСЏ РІС‹РїРѕР»РЅРµРЅРёСЏ.</returns>
    public async Task AbortExecution() => await ActionExecutor.StopAsync(_stopDelegate, _userActionTcs);

    /// <summary>
    /// РќР°С‡РёРЅР°РµС‚ Р·Р°РїСѓСЃРє РёР·РјРµСЂРµРЅРёСЏ.
    /// </summary>
    /// <returns>Р—Р°РґР°С‡Р°, РїСЂРµРґСЃС‚Р°РІР»СЏСЋС‰Р°СЏ Р°СЃРёРЅС…СЂРѕРЅРЅСѓСЋ РѕРїРµСЂР°С†РёСЋ РёР·РјРµСЂРµРЅРёСЏ.</returns>
    public async Task StartAsync() => await ActionExecutor.StartAsync(_startDelegate, _stopDelegate, header.Text, _isRepeatEnabled, _preActionDelegate, _checkPower);

    /// <summary>
    /// Р—Р°РІРµСЂС€РµРЅРёРµ С‚РµРєСѓС‰РµР№ РІС‹РїРѕР»РЅСЏРµРјРѕР№ Р·Р°РґР°С‡Рё.
    /// </summary>
    /// <returns>Р—Р°РґР°С‡Р°, РїСЂРµРґСЃС‚Р°РІР»СЏСЋС‰Р°СЏ Р°СЃРёРЅС…СЂРѕРЅРЅСѓСЋ РѕРїРµСЂР°С†РёСЋ Р·Р°РІРµСЂС€РµРЅРёСЏ.</returns>
    private async Task StopAsync() => await ActionExecutor.StopAsync(_stopDelegate, _userActionTcs);

    /// <summary>
    /// Р’С‹РїРѕР»РЅСЏРµС‚ Р·Р°РІРµСЂС€Р°СЋС‰РёРµ РґРµР№СЃС‚РІРёСЏ РїРѕСЃР»Рµ Р·Р°РІРµСЂС€РµРЅРёСЏ РїСЂРѕС†РµСЃСЃР°.
    /// </summary>
    /// <param name="stopDelegate">Р”РµР»РµРіР°С‚ Р·Р°РІРµСЂС€РµРЅРёСЏ РїСЂРѕС†РµСЃСЃР° (РЅРµРѕР±СЏР·Р°С‚РµР»СЊРЅРѕ).</param>
    /// <returns>Р—Р°РґР°С‡Р°, РїСЂРµРґСЃС‚Р°РІР»СЏСЋС‰Р°СЏ Р°СЃРёРЅС…СЂРѕРЅРЅСѓСЋ РѕРїРµСЂР°С†РёСЋ Р·Р°РІРµСЂС€РµРЅРёСЏ.</returns>
    public async Task FinalizeAsync(StopDelegate stopDelegate = null) => await ActionExecutor.FinalizeAsync(stopDelegate);

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

    #endregion

    #region РџРѕРІС‚РѕСЂ Рё Р·Р°С†РёРєР»РёРІР°РЅРёРµ.

    /// <summary>
    /// Р—Р°РїСѓСЃРєР°РµС‚ С†РёРєР» РІС‹РїРѕР»РЅРµРЅРёСЏ РґРµР»РµРіР°С‚Р° РёР·РјРµСЂРµРЅРёСЏ, РѕС‚РѕР±СЂР°Р¶Р°СЏ РєРЅРѕРїРєРё "РћСЃС‚Р°РЅРѕРІРёС‚СЊ" Рё "Р—Р°РІРµСЂС€РёС‚СЊ".
    /// </summary>
    private async void LoopMeasureEvent() => await ActionExecutor.LoopMeasureEvent(_returnDelegate, _stopDelegate);

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
    /// <param name="showMessageModel">РњРѕРґРµР»СЊ СЃРѕРѕР±С‰РµРЅРёСЏ.</param>
    /// <returns>Р’РѕР·РІСЂР°С‰Р°РµС‚ СЂРµР¶РёРј РїРѕ С€Р°РіР°Рј.</returns>
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
        if (showMessageModel.Debug == " ")
        {
          // Пробел используется для строк MKI-протокола, где C#-путь вызова не должен выводиться.
        }
        else if (string.IsNullOrEmpty(showMessageModel.Debug))
        {
          showMessageModel.Debug = $"{Path.GetFileName(callerFile)} -> {callerName} (строка {callerLine})";
        }
        else
        {
          showMessageModel.Debug += $"|| {Path.GetFileName(callerFile)} -> {callerName} (строка {callerLine})";
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
      await MoveToLineAsync(item.SourceLineNumber);
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂСЏРµС‚ СЃС‚Р°С‚СѓСЃ СЃРѕРѕР±С‰РµРЅРёСЏ Рё РґРѕР±Р°РІР»СЏРµС‚ С‚РµРєСЃС‚РѕРІСѓСЋ РїСЂРёСЃС‚Р°РІРєСѓ Рё С†РІРµС‚, РµСЃР»Рё СЃС‚Р°С‚СѓСЃ РЅРµ СЏРІР»СЏРµС‚СЃСЏ РёРЅС„РѕСЂРјР°С†РёРѕРЅРЅС‹Рј.
    /// </summary>
    /// <param name="showMessageModel">РњРѕРґРµР»СЊ РѕС‚РѕР±СЂР°Р¶Р°РµРјРѕРіРѕ СЃРѕРѕР±С‰РµРЅРёСЏ, РїРµСЂРµРґР°С‘С‚СЃСЏ РїРѕ СЃСЃС‹Р»РєРµ.</param>
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
    /// Р•СЃР»Рё СЃС‚Р°С‚СѓСЃ СЃРѕРѕР±С‰РµРЅРёСЏ вЂ” РѕС€РёР±РєР° Рё РІРєР»СЋС‡РµРЅР° РѕСЃС‚Р°РЅРѕРІРєР° РїСЂРё РѕС€РёР±РєРµ, РІС‹РїРѕР»РЅРµРЅРёРµ СЃС‚Р°РІРёС‚СЃСЏ РЅР° РїР°СѓР·Сѓ.
    /// </summary>
    /// <param name="Status">РўРёРї СЃРѕРѕР±С‰РµРЅРёСЏ (РѕС€РёР±РєР°, РёРЅС„РѕСЂРјР°С†РёСЏ, СѓСЃРїРµС…).</param>
    private async Task CheckPause(ShowMessageModel.MessageType? Status)
    {
      if (Status == MessageType.Error && await ExecutionConfig.GetIsStopOnErrorEnabled())
      {
        await PauseAsync();
      }
    }

    /// <summary>
    /// РџСЂРѕРІРµСЂСЏРµС‚, РЅСѓР¶РЅРѕ Р»Рё РѕС‚РѕР±СЂР°Р¶Р°С‚СЊ РґРµС‚Р°Р»РёР·РёСЂРѕРІР°РЅРЅС‹Р№ РїСЂРѕС‚РѕРєРѕР».
    /// Р•СЃР»Рё РЅРµ РЅСѓР¶РЅРѕ, СѓРґР°Р»СЏРµС‚ РїРѕСЃР»РµРґРЅРµРµ СЃРѕРѕР±С‰РµРЅРёРµ, РµСЃР»Рё РѕРЅРѕ РґРѕРїСѓСЃРєР°РµС‚ СѓРґР°Р»РµРЅРёРµ Рё РЅРµ СЃРѕРґРµСЂР¶РёС‚ РѕС€РёР±РєРё РІС‹РїРѕР»РЅРµРЅРёСЏ.
    /// </summary>
    /// <param name="showMessageModel">РњРѕРґРµР»СЊ С‚РµРєСѓС‰РµРіРѕ СЃРѕРѕР±С‰РµРЅРёСЏ, РєРѕС‚РѕСЂРѕРµ РїРѕС‚РµРЅС†РёР°Р»СЊРЅРѕ Р±СѓРґРµС‚ СЃРѕС…СЂР°РЅРµРЅРѕ РєР°Рє РїРѕСЃР»РµРґРЅРµРµ.</param>
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
    /// РџРѕР»РЅРѕСЃС‚СЊСЋ РѕС‡РёС‰Р°РµС‚ РїСЂРѕС‚РѕРєРѕР» Рё СЃР±СЂР°СЃС‹РІР°РµС‚ РїРѕСЃР»РµРґРЅРµРµ СЃРѕРѕР±С‰РµРЅРёРµ.
    /// </summary>
    /// <returns>Р’РѕР·РІСЂР°С‰Р°РµС‚ РїСЂРёР·РЅР°Рє СѓСЃРїРµС€РЅРѕРіРѕ Р·Р°РІРµСЂС€РµРЅРёСЏ РѕРїРµСЂР°С†РёРё.</returns>
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
    /// РђСЃРёРЅС…СЂРѕРЅРЅРѕ СѓРґР°Р»СЏРµС‚ Р±Р»РѕРє, СЃРѕРґРµСЂР¶Р°С‰РёР№ СѓРєР°Р·Р°РЅРЅСѓСЋ СЃС‚СЂРѕРєСѓ, РёР· RichTextBox.
    /// </summary>
    /// <param name="textToRemove">РЎС‚СЂРѕРєР° РґР»СЏ РїРѕРёСЃРєР° Рё СѓРґР°Р»РµРЅРёСЏ.</param>
    /// <returns>True, РµСЃР»Рё Р±Р»РѕРє Р±С‹Р» РЅР°Р№РґРµРЅ Рё СѓРґР°Р»РµРЅ; РёРЅР°С‡Рµ False.</returns>
    public async Task<bool> RemoveLineContainingTextAsync(string textToRemove) => await protocolTextBox.RemoveLineContainingTextAsync(textToRemove);

    /// <summary>
    /// РЎРѕС…СЂР°РЅСЏРµС‚ РїСЂРѕС‚РѕРєРѕР» РІ С„Р°Р№Р» СЃ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРё СЃРіРµРЅРµСЂРёСЂРѕРІР°РЅРЅС‹Рј РёРјРµРЅРµРј РІ С„РѕРЅРѕРІРѕРј СЂРµР¶РёРјРµ Р°СЃРёРЅС…СЂРѕРЅРЅРѕ.
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
    /// РњРµС‚РѕРґ СЃРѕР·РґР°С‘С‚ РЅРѕРІС‹Р№ <see cref="TaskCompletionSource{TResult}"/> РґР»СЏ РѕР¶РёРґР°РЅРёСЏ РІС‹Р±РѕСЂР° РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ 
    /// (РЅР°РїСЂРёРјРµСЂ, РїСЂРѕРґРѕР»Р¶РёС‚СЊ, РїСЂРѕРїСѓСЃС‚РёС‚СЊ РёР»Рё РѕСЃС‚Р°РЅРѕРІРёС‚СЊ РІС‹РїРѕР»РЅРµРЅРёРµ).  
    /// Р•СЃР»Рё РІ РєРѕРЅС„РёРіСѓСЂР°С†РёРё СѓСЃС‚Р°РЅРѕРІР»РµРЅРѕ СЃРІРѕР№СЃС‚РІРѕ <c>IsStopOnErrorEnabled</c>,  
    /// РёРЅС‚РµСЂС„РµР№СЃ РїРµСЂРµС…РѕРґРёС‚ РІ СЂРµР¶РёРј РїР°СѓР·С‹ вЂ” СЃРєСЂС‹РІР°СЋС‚СЃСЏ РІСЃРµ РєРЅРѕРїРєРё Рё РѕС‚РѕР±СЂР°Р¶Р°СЋС‚СЃСЏ РєРЅРѕРїРєРё СѓРїСЂР°РІР»РµРЅРёСЏ РїР°СѓР·РѕР№.  
    /// РџРѕСЃР»Рµ РІС‹Р±РѕСЂР° РґРµР№СЃС‚РІРёСЏ РїРѕР»СЊР·РѕРІР°С‚РµР»РµРј СЂРµР·СѓР»СЊС‚Р°С‚ РІРѕР·РІСЂР°С‰Р°РµС‚СЃСЏ РєР°Рє Р·РЅР°С‡РµРЅРёРµ РїРµСЂРµС‡РёСЃР»РµРЅРёСЏ 
    /// <see cref="IUserInteractionService.UserAction"/>.
    /// </remarks>
    /// <returns>
    /// Р—Р°РґР°С‡Р°, РїСЂРµРґСЃС‚Р°РІР»СЏСЋС‰Р°СЏ РѕР¶РёРґР°РµРјРѕРµ РґРµР№СЃС‚РІРёРµ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ.  
    /// Р•СЃР»Рё СЂРµР¶РёРј РѕСЃС‚Р°РЅРѕРІРєРё РЅР° РѕС€РёР±РєРµ РѕС‚РєР»СЋС‡С‘РЅ, РІРѕР·РІСЂР°С‰Р°РµС‚СЃСЏ <see cref="IUserInteractionService.UserAction.None"/>.
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
