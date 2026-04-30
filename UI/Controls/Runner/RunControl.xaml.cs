using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.View.EditorHost;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.UI.Controls.ProtocolNew;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using UI.Controls.TextEditorControl;
using Ask.UI.Controls.ErrorList;
using Ask.UI.Controls.ProtocolNew;
using UI.Services;
using UI.Services.Archive;
using UI.Windows.WpfDocking.Windows.Docking;
using UI.Windows.WpfDocking.Windows.Docking.Primitives;
using static Ask.LogLib.LoggerUtility;

namespace UI.Controls.Runner
{
  /// <summary>
  /// Логика взаимодействия для RunControl.xaml
  /// </summary>
  public partial class RunControl : UserControl, IRunView
  {
    /// <summary>
    /// Флаг, указывающий, находится ли интерфейс в заблокированном состоянии.
    /// Используется для предотвращения повторного применения изменений.
    /// </summary>
    private static bool isLocked = false;

    private List<BaseCommandModel> ControlProgram = null;

    private bool _userResizing = false;
    private bool _resizeRefreshQueued = false;

    public int ErrorCount { get; private set; } = 0;
    public int TranslationErrorCount { get; private set; } = 0;

    private ProtocolUI ProtocolUI { get; set; }

    public string FileName { get; set; }

    public string OpkFilePath { get; set; }

    private List<BaseCommandModel> translationModels = new List<BaseCommandModel>();

    private TextEditorContainer _leftEditor;
    private readonly ArchiveSaveService _archiveSaveService = new ArchiveSaveService();
    private readonly TranslatedFileSaveService _translatedFileSaveService = new TranslatedFileSaveService();

    public List<BaseCommandModel> TranslationModels
    {
      get
      {
        return translationModels;
      }
      set
      {
        translationModels = value ?? new List<BaseCommandModel>();
        ErrorClear();
        TranslationErrorCount = 0;

        foreach (var model in translationModels)
        {
          if (model.Errors.Count > 0)
          {
            SetTranslationError(model.Errors);
          }
        }

        UpdateTranslatorEditorActions();
        UpdateArchiveButtonVisibility();
      }
    }

    public UserControl View => this;

    private bool task = false;

    private DevicesStatus devicesStatus;
    public RunControl()
    {
      InitializeComponent();
      ProtocolUI = new ProtocolUI(true);
      ProtocolUI.ErrorListBoxVerticalVisibility = Visibility.Collapsed;
      MainContent.Content = ProtocolUI;
      ErrorListBoxVertical.ItemDoubleClicked += ErrorItemDoubleClicked;
      ErrorListBoxVertical.DesiredHeightChanged += ErrorListBoxVertical_DesiredHeightChanged;
      devicesStatus = new DevicesStatus();

      EventAggregator.Subscribe<SystemStateEvents.LockedChanged>(e => OnLockedChanged(e.IsLocked));
      EventAggregator.Subscribe<ExecutionEvents.ActiveDeviceChanged>(e => devicesStatus.LoadDevices(e.Devices));

      Loaded += RunControl_Loaded;
      LeftBox.AddHandler(UIElement.PreviewGotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(LeftBox_PreviewGotKeyboardFocus), true);

      // Вкладка "Точки остановки"
      ErrorListBoxVertical.BreakpointItemDoubleClicked += BreakpointItemDoubleClicked;
      ErrorListBoxVertical.BreakpointEnabledChanged += BreakpointEnabledChanged;

      // События брейкпоинтов, чтобы вкладка обновлялась в Run
      EventAggregator.Subscribe<BreakpointEvents.BreakpointSet>(e => OnBreakpointSet(e));
      EventAggregator.Subscribe<BreakpointEvents.BreakpointRemoved>(e => OnBreakpointRemoved(e));
      EventAggregator.Subscribe<BreakpointEvents.BreakpointOn>(e => OnBreakpointOn(e));
      EventAggregator.Subscribe<BreakpointEvents.BreakpointOff>(e => OnBreakpointOff(e));
    }
    /// <summary>
    /// Обрабатывает событие изменения состояния блокировки интерфейса.
    /// Скрывает или отображает верхнюю панель окна в зависимости от нового значения.
    /// </summary>
    /// <param name="newValue">Новое состояние блокировки: <c>true</c> — интерфейс заблокирован; <c>false</c> — разблокирован.</param>
    private void OnLockedChanged(bool newValue)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        var translatorItem = ChildTextEditorContainer.DockManager.DockItems.FirstOrDefault(item => item.Content.GetType() is TranslatorEditor);
        if (translatorItem != null)
        {
          var translatorEditor = translatorItem.Content as TranslatorEditor;
          var canReturnToSource = CanReturnToSourceFile();
          if (newValue)
          {
            translatorEditor.BackButton.Visibility = Visibility.Collapsed;
            isLocked = true;
          }

          else
          {
            translatorEditor.BackButton.Visibility = canReturnToSource ? Visibility.Visible : Visibility.Collapsed;
            isLocked = false;
          }
        }
      });
    }

    private async void ErrorItemDoubleClicked(IDisplayIssue obj)
    {
      var protocolUI = MainContent.Content as ProtocolUI;
      if (protocolUI != null)
      {
        if (obj.SourceLineNumber >= 0)
        {
          await protocolUI.MoveToLineAsync(obj.SourceLineNumber);
        }

        if (obj.FormattedLineNumber >= 0)
        {
          var dockManader = ChildTextEditorContainer.DockManager;
          var dockItemPk = dockManader.DockItems.FirstOrDefault(di => di.TabText != "Состояние оборудования");
          if (dockItemPk != null && dockItemPk.Content is TextEditorUI textEditor)
          {
            textEditor.GoToLine(obj.FormattedLineNumber);
          }
        }
      }
    }

    private void RunControl_Loaded(object sender, RoutedEventArgs e)
    {
      ApplyErrorListHeight(ErrorListLayoutSettings.GetInitialHeight());
      ErrorListBoxVertical.RefreshLayoutFromHost();
      FocusMainContent();
    }
    private void LeftBox_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
      e.Handled = true;
      FocusMainContent();
    }
    private void FocusMainContent()
    {
      if (MainContent.Content is IInputElement focusable && focusable.Focusable)
      {
        Keyboard.Focus(focusable);
      }
      else if (MainContent.Content is FrameworkElement fe)
      {
        fe.Loaded += (_, _) =>
        {
          fe.Focus();
          Keyboard.Focus(fe);
        };
      }
    }

    private void SetTranslationError(List<ErrorItem> errorItems)
    {
      if (errorItems == null || errorItems.Count == 0)
        return;

      ErrorListBoxVertical.AddErrors(errorItems);
      ErrorCount += errorItems.Count;
      TranslationErrorCount += errorItems.Count;

      if (ErrorCount > 0)
      {
        MessageEventAdapter.RaiseInfoMessage($"Общее кол-во ошибок: {ErrorCount}");
      }
    }


    public void SetLeftEditor(TextEditorUI textEditorUI)
    {
      LogInformation("SetLeftEditor вызван: " + this.GetHashCode());
      if (textEditorUI == null)
        return;

      if (textEditorUI.Parent is Panel oldParent)
      {
        oldParent.Children.Remove(textEditorUI);
      }
      else if (textEditorUI.Parent is ContentControl oldContent)
      {
        oldContent.Content = null;
      }
      else if (textEditorUI.Parent is Decorator decorator)
      {
        decorator.Child = null;
      }

      var dockManager = ChildTextEditorContainer.DockManager;
      if (dockManager.DockItems.Count > 0)
      {
        foreach (var dockItem in dockManager.DockItems.ToList())
        {
          dockManager.DockItems.Remove(dockItem);
        }
      }

      var rightEditor = new TranslatorEditor();
      rightEditor.SetEditor(textEditorUI);
      rightEditor.BackRequested += TranslatorEditor_BackRequested;
      rightEditor.SaveRequested -= RightEditor_SaveRequestedAsync;
      rightEditor.SaveRequested += RightEditor_SaveRequestedAsync;
      rightEditor.SaveToDiskRequested -= RightEditor_SaveToDiskRequestedAsync;
      rightEditor.SaveToDiskRequested += RightEditor_SaveToDiskRequestedAsync;
      rightEditor.SetArchiveButtonVisibility(ErrorCount == 0);
      rightEditor.BackButton.Visibility = CanReturnToSourceFile() && !isLocked
        ? Visibility.Visible
        : Visibility.Collapsed;
      var fileName = GetDisplayFileName(textEditorUI.TextEditorModel.FilePath, textEditorUI.TextEditorModel.FileName);
      var filePath = textEditorUI.TextEditorModel.FilePath;
      rightEditor.TranslationFileName.Text = fileName;
      rightEditor.SetSaveToDiskVisible(translationModels.Count > 0 && ErrorCount == 0);
      var dockItemPk = new DockItem
      {
        Title = fileName,
        TabText = fileName,
        Content = rightEditor,
      };

      var dockItemDeviceState = new DockItem
      {
        Title = filePath,
        TabText = "Состояние оборудования",
        Content = devicesStatus,
      };

      DocumentTab.SetHideCloseButton(dockItemPk, true);
      DocumentTab.SetHideCloseButton(dockItemDeviceState, true);

      dockManager.DockItems.Add(dockItemPk);
      dockManager.DockItems.Add(dockItemDeviceState);

      LogInformation($"Попытка показать ChildTextEditorContainer.DockItem. Title: {dockItemPk.Title}, IsLoaded: {dockManager.IsLoaded}, DockItems.Count: {dockManager.DockItems.Count}");

      if (!dockManager.IsLoaded)
      {
        LogWarning("ChildTextEditorContainer.DockControl ещё не загружен. Подписка на Loaded...");

        var capturedDockItem = dockItemPk;
        dockManager.Loaded += (s, e) =>
        {
          try
          {
            LogInformation("ChildTextEditorContainer.DockControl загрузился. Показываем вкладку.");
            LogInformation("ChildTextEditorContainer.DockItem отображён после загрузки.");
            var isControlProgramActive = true;

            SystemStateManager.SetIsControlProgramActive(isControlProgramActive);
            dockItemDeviceState.Show(dockManager, DockPosition.Document);
            capturedDockItem.Show(dockManager, DockPosition.Document);

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
              SyncBreakpointsFromEditor(textEditorUI);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
          }
          catch (Exception ex)
          {
            LogException("Ошибка при отображении ChildTextEditorContainer.DockItem после загрузки:", ex);
          }
        };
      }
      else
      {
        var isControlProgramActive = true;

        SystemStateManager.SetIsControlProgramActive(isControlProgramActive);

        dockItemDeviceState.Show(dockManager, DockPosition.Document);
        dockItemPk.Show(dockManager, DockPosition.Document);

        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
          SyncBreakpointsFromEditor(textEditorUI);
        }), System.Windows.Threading.DispatcherPriority.Loaded);

        LogInformation("ChildTextEditorContainer.DockItem отображён немедленно.");
      }
    }


    public async Task Start(List<BaseCommandModel> models)
    {
      ProtocolUI.MenuButtonVisibility(false);
      ControlProgram = models;

      if (models.Count == 0 || models[0].Mnemonic != "ОК")
      {
        return;
      }

      ProtocolUI.Header = BuildDerivedFileName(OpkFilePath, FileName, ".lst", "protocol.lst");
      ProtocolUI.SetSettings(StartDelegate: StartTest, false);
      this.FileName = ProtocolUI.Header;

      await ProtocolUI.StartAsync();
    }

    private async Task StartTest(IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      TextEditorUI? editor = null;

      Application.Current.Dispatcher.Invoke(() =>
      {
        var dockManager = ChildTextEditorContainer.DockManager;
        var dockItem = dockManager.DockItems.FirstOrDefault(di => di.Content is TranslatorEditor);
        if (dockItem != null)
        {
          var translatorEditor = dockItem.Content as TranslatorEditor;
          editor = translatorEditor?.GetTextEditor();
        }
      });

      var manager = new CommandExecutionManager(ProtocolUI, editor, ControlProgram, OpkFilePath);
      manager.ClearError += ErrorClear;
      manager.AddError += AddError;

      await manager.ExecuteAllAsync();
    }

    private bool CanReturnToSourceFile()
    {
      if (string.IsNullOrWhiteSpace(OpkFilePath))
      {
        return true;
      }

      var extension = Path.GetExtension(OpkFilePath);
      return !string.Equals(extension, ".opk", StringComparison.OrdinalIgnoreCase);
    }

    private void AddError(ErrorItem errorItem)
    {
      Application.Current.Dispatcher?.Invoke(() =>
      {
        ErrorListBoxVertical.AddError(errorItem);
        ErrorCount++;

        if (ErrorCount > 0)
        {
          MessageEventAdapter.RaiseInfoMessage($"Общее кол-во ошибок: {ErrorCount}");
        }
      });
    }

    private void ErrorClear()
    {
      Application.Current.Dispatcher?.Invoke(() =>
      {
        ErrorListBoxVertical.ClearAll();
        ErrorCount = 0;
        UpdateArchiveButtonVisibility();
      });
    }

    private void UpdateArchiveButtonVisibility()
    {
      Application.Current.Dispatcher?.Invoke(() =>
      {
        var translatorEditor = ChildTextEditorContainer.DockManager.DockItems
          .FirstOrDefault(item => item.Content is TranslatorEditor)?
          .Content as TranslatorEditor;
        if (translatorEditor != null)
        {
          translatorEditor.SetArchiveButtonVisibility(ErrorCount == 0);
          translatorEditor.SetSaveToDiskVisible(CanSaveTranslatedFileToDisk());
        }
      });
    }

    private void TranslatorEditor_BackRequested(object? sender, EventArgs e)
    {
      var textEditorContainer = LeftBox.Children.Count > 0 ? LeftBox.Children[0] as TextEditorContainer : null;
      TranslatorNavigationService.TryOpenSourceFileFromTranslator(
        textEditorContainer,
        onSourceOpened: () => EditorEventAdapter.RaiseCloseRunItem(this));
    }

    private void RightEditor_SaveRequestedAsync(object? sender, EventArgs e)
    {
      var rightEditor = sender as TranslatorEditor;
      var sourceFilePath = rightEditor?.GetTextEditor()?.TextEditorModel?.FilePath;
      if (string.IsNullOrWhiteSpace(sourceFilePath))
      {
        sourceFilePath = OpkFilePath;
      }

      var sourceText = TryReadSourceTextForArchive(sourceFilePath)
        ?? rightEditor?.GetTextEditor()?.Text
        ?? string.Empty;

      _archiveSaveService.SaveFileToArchive(this, sourceText, sourceFilePath);
    }

    private static string? TryReadSourceTextForArchive(string? sourceFilePath)
    {
      if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
      {
        return null;
      }

      var ext = Path.GetExtension(sourceFilePath);
      var encoding = string.Equals(ext, ".pkw", StringComparison.OrdinalIgnoreCase)
        || string.Equals(ext, ".opkw", StringComparison.OrdinalIgnoreCase)
          ? new UTF8Encoding(false)
          : Encoding.GetEncoding(866);

      return File.ReadAllText(sourceFilePath, encoding);
    }

    private void RightEditor_SaveToDiskRequestedAsync(object? sender, EventArgs e)
    {
      var rightEditor = sender as TranslatorEditor;
      var translatedText = rightEditor?.GetTextEditor()?.Text;
      var sourceFilePath = rightEditor?.GetTextEditor()?.TextEditorModel?.FilePath;
      if (string.IsNullOrWhiteSpace(sourceFilePath))
      {
        sourceFilePath = OpkFilePath;
      }

      _translatedFileSaveService.SaveToDisk(this, translatedText ?? string.Empty, sourceFilePath);
    }

    private void BottomSplitter_OnDragStarted(object sender, DragStartedEventArgs e)
    {
      _userResizing = true;

      ApplyErrorListBounds();
      BottomRow.Height = new GridLength(ErrorListLayoutSettings.ClampHeight(ErrorListBoxVertical.ActualHeight));
    }

    private void BottomSplitter_OnDragDelta(object sender, DragDeltaEventArgs e)
    {
      ClampErrorListRowDuringResize();
      QueueErrorListResizeRefresh();
    }

    private void BottomSplitter_OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
      _userResizing = false;

      var height = ErrorListLayoutSettings.ClampHeight(ErrorListBoxVertical.ActualHeight);
      ApplyErrorListHeight(height);
      ErrorListLayoutSettings.SaveHeight(height);
      ErrorListBoxVertical.RefreshLayoutFromHost();
    }

    private void QueueErrorListResizeRefresh()
    {
      if (_resizeRefreshQueued)
        return;

      _resizeRefreshQueued = true;
      Dispatcher.BeginInvoke(
        new Action(() =>
        {
          _resizeRefreshQueued = false;
          ErrorListBoxVertical.RefreshLayoutFromHost();
        }),
        DispatcherPriority.Render);
    }

    private void ErrorListBoxVertical_DesiredHeightChanged(double height)
    {
      if (_userResizing)
        return;

      ApplyErrorListHeight(height);
    }

    private void ApplyErrorListHeight(double height)
    {
      var clampedHeight = ErrorListLayoutSettings.ClampHeight(height);

      ApplyErrorListBounds();
      BottomRow.Height = new GridLength(clampedHeight);
    }

    private void ClampErrorListRowDuringResize()
    {
      var clampedHeight = ErrorListLayoutSettings.ClampHeight(BottomRow.ActualHeight);
      if (Math.Abs(BottomRow.ActualHeight - clampedHeight) < 0.5)
        return;

      BottomRow.Height = new GridLength(clampedHeight);
    }

    private void ApplyErrorListBounds()
    {
      var minHeight = ErrorListLayoutSettings.GetMinHeight();
      var maxHeight = ErrorListLayoutSettings.GetMaxHeight();

      ErrorListBoxVertical.MinHeight = minHeight;
      BottomRow.MinHeight = minHeight;

      if (double.IsInfinity(maxHeight))
      {
        ErrorListBoxVertical.ClearValue(MaxHeightProperty);
        BottomRow.ClearValue(RowDefinition.MaxHeightProperty);
      }
      else
      {
        ErrorListBoxVertical.MaxHeight = maxHeight;
        BottomRow.MaxHeight = maxHeight;
      }
    }

    private bool CanSaveTranslatedFileToDisk()
    {
      return translationModels.Count > 0 && TranslationErrorCount == 0;
    }

    private static string GetDisplayFileName(string? filePath, string? fileName)
    {
      if (!string.IsNullOrWhiteSpace(fileName))
      {
        return fileName;
      }

      return string.IsNullOrWhiteSpace(filePath)
        ? string.Empty
        : Path.GetFileName(filePath);
    }

    private static string BuildDerivedFileName(string? sourceFilePath, string? sourceFileName, string extension, string fallbackFileName)
    {
      string baseName = Path.GetFileNameWithoutExtension(sourceFilePath);
      if (string.IsNullOrWhiteSpace(baseName))
      {
        baseName = Path.GetFileNameWithoutExtension(sourceFileName);
      }

      return string.IsNullOrWhiteSpace(baseName)
        ? fallbackFileName
        : $"{baseName}{extension}";
    }

    private TranslatorEditor? GetTranslatorEditor()
    {
      return ChildTextEditorContainer.DockManager.DockItems
        .FirstOrDefault(item => item.Content is TranslatorEditor)?
        .Content as TranslatorEditor;
    }

    private void UpdateTranslatorEditorActions()
    {
      var translatorEditor = GetTranslatorEditor();
      if (translatorEditor == null)
      {
        return;
      }

      translatorEditor.SetSaveToDiskVisible(CanSaveTranslatedFileToDisk());
    }

    /// <summary>
    /// Синхронизирует список точек остановки во вкладке
    /// с текущим состоянием редактора выполнения.
    /// </summary>
    /// <param name="editor">Экземпляр <see cref="TextEditorUI"/>, содержащий активные точки остановки.</param>
    /// <remarks>
    /// Метод полностью очищает текущий список точек остановки во вкладке,
    /// затем заново формирует его на основе данных редактора.
    /// </remarks>
    private void SyncBreakpointsFromEditor(TextEditorUI editor)
    {
      if (editor == null)
        return;

      ErrorListBoxVertical.ClearBreakpoints();

      var doc = editor.Document;
      var cmdNumbers = editor.BreakpointCommandsNumbers;
      var anchors = editor.BreakPointLines;

      int count = Math.Min(cmdNumbers.Count, anchors.Count);

      for (int i = 0; i < count; i++)
      {
        int cmd = cmdNumbers[i];
        int line1 = doc.GetLineByOffset(anchors[i].Offset).LineNumber;
        bool enabled = editor.IsBreakpointEnabled(cmd);

        ErrorListBoxVertical.UpsertBreakpoint(cmd, line1, editor.NumCommandWithMnemonic[cmd], enabled);
        SetModelBreakpoint(cmd, has: true, enabled: enabled);
      }
    }

    /// <summary>
    /// Обрабатывает двойной щелчок по элементу списка точек остановки.
    /// </summary>
    /// <param name="bp">Элемент списка точек остановки.</param>
    /// <remarks>
    /// При наличии корректного номера строки (1-based)
    /// выполняется переход редактора к соответствующей строке.
    /// </remarks>
    private void BreakpointItemDoubleClicked(BreakpointListItem bp)
    {
      var editor = ChildTextEditorContainer.GetTextEditor();
      if (editor == null)
        return;

      if (bp.RightLine.HasValue && bp.RightLine.Value > 0)
        editor.ScrollToLine(bp.RightLine.Value);
    }

    /// <summary>
    /// Обрабатывает изменение состояния (включена/выключена)
    /// точки остановки из списка.
    /// </summary>
    /// <param name="bp">Элемент точки остановки.</param>
    /// <param name="enabled">Состояние точки остановки.</param>
    /// <remarks><see langword="true"/> для включения точки остановки.</remarks>
    private void BreakpointEnabledChanged(BreakpointListItem bp, bool enabled)
    {
      var editor = ChildTextEditorContainer.GetTextEditor();
      if (editor == null)
        return;

      if (!editor.HasBreakpointCommand(bp.CommandNumber))
        return;

      if (editor.IsBreakpointEnabled(bp.CommandNumber) == enabled)
        return;

      if (enabled)
        editor.EnableBreakpoint(bp.CommandNumber, raiseEvents: true);
      else
        editor.DisableBreakpoint(bp.CommandNumber, raiseEvents: true);
    }

    /// <summary>
    /// Обрабатывает событие установки новой точки остановки.
    /// </summary>
    /// <param name="e">Аргументы события установки точки остановки.</param>
    private void OnBreakpointSet(BreakpointEvents.BreakpointSet e)
    {
      var editor = ChildTextEditorContainer.GetTextEditor();
      if (editor == null)
        return;

      int line1 = GetLine1BasedByCommand(editor, e.CommandNumber);
      ErrorListBoxVertical.UpsertBreakpoint(e.CommandNumber, line1, editor.NumCommandWithMnemonic[e.CommandNumber], isEnabled: true);

      SetModelBreakpoint(e.CommandNumber, has: true, enabled: true);
    }

    /// <summary>
    /// Обрабатывает событие удаления точки остановки.
    /// </summary>
    /// <param name="e">Аргументы события удаления точки остановки.</param>
    private void OnBreakpointRemoved(BreakpointEvents.BreakpointRemoved e)
    {
      ErrorListBoxVertical.RemoveBreakpoint(e.CommandNumber);
      SetModelBreakpoint(e.CommandNumber, has: false, enabled: true);
    }

    /// <summary>
    /// Обрабатывает событие включения точки остановки.
    /// </summary>
    /// <param name="e">Аргументы события выключения точки остановки.</param>
    private void OnBreakpointOn(BreakpointEvents.BreakpointOn e)
    {
      var editor = ChildTextEditorContainer.GetTextEditor();
      if (editor == null)
        return;

      int line1 = GetLine1BasedByCommand(editor, e.CommandNumber);
      ErrorListBoxVertical.UpsertBreakpoint(e.CommandNumber, line1, editor.NumCommandWithMnemonic[e.CommandNumber], isEnabled: true);

      SetModelBreakpoint(e.CommandNumber, has: true, enabled: true);
    }

    /// <summary>
    /// Обрабатывает событие выключения точки остановки.
    /// </summary>
    /// <param name="e">Аргументы события выключения точки остановки.</param>
    private void OnBreakpointOff(BreakpointEvents.BreakpointOff e)
    {
      var editor = ChildTextEditorContainer.GetTextEditor();
      if (editor == null)
        return;

      int line1 = GetLine1BasedByCommand(editor, e.CommandNumber);
      ErrorListBoxVertical.UpsertBreakpoint(e.CommandNumber, line1, editor.NumCommandWithMnemonic[e.CommandNumber], isEnabled: false);

      SetModelBreakpoint(e.CommandNumber, has: true, enabled: false);
    }

    /// <summary>
    /// Возвращает 1-based номер строки документа,
    /// соответствующий указанной команде.
    /// </summary>
    /// <param name="editor">Редактор, содержащий точки остановки.</param>
    /// <param name="commandNumber">Номер команды.</param>
    /// <returns>
    /// Номер строки документа (1-based),
    /// на которой расположена точка остановки.
    /// </returns>
    private static int GetLine1BasedByCommand(TextEditorUI editor, int commandNumber)
    {
      var doc = editor.Document;
      int idx = editor.BreakpointCommandsNumbers.IndexOf(commandNumber);
      return doc.GetLineByOffset(editor.BreakPointLines[idx].Offset).LineNumber; // 1-based
    }

    /// <summary>
    /// Обновляет состояние точки остановки
    /// в модели команды трансляции.
    /// </summary>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="has">
    /// Признак наличия точки остановки.
    /// </param>
    /// <param name="enabled">
    /// Признак включённого состояния точки остановки.
    /// </param>
    private void SetModelBreakpoint(int commandNumber, bool has, bool enabled)
    {
      var model = translationModels.FirstOrDefault(m =>
        int.TryParse(m.CommandNumber, out var num) && num == commandNumber);

      if (model == null) return;

      model.HasBreakpoint = has;
      model.IsBreakpointEnabled = enabled;
    }
  }
}
