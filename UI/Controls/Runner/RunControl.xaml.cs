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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using UI.Controls.ErrorList;
using Ask.UI.Controls.ProtocolNew;
using UI.Controls.TextEditor;
using UI.Services;
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

    private const double MaxAutoHeight = 250.0;

    public int ErrorCount { get; private set; } = 0;

    private ProtocolUI ProtocolUI { get; set; }

    public string FileName { get; set; }

    public string OpkFilePath { get; set; }

    private List<BaseCommandModel> translationModels = new List<BaseCommandModel>();

    private TextEditorContainer _leftEditor;
    public List<BaseCommandModel> TranslationModels
    {
      get
      {
        return translationModels;
      }
      set
      {
        translationModels = value;
        ErrorClear();

        foreach (var model in value)
        {
          if (model.Errors.Count > 0)
          {
            SetError(model.Errors);
          }
        }
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
          if (newValue)
          {
            translatorEditor.BackButton.Visibility = Visibility.Collapsed;
            isLocked = true;
          }

          else
          {
            translatorEditor.BackButton.Visibility = Visibility.Visible;
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

    private void SetError(List<ErrorItem> errorItems)
    {
      foreach (ErrorItem errorItem in errorItems)
      {
        ErrorListBoxVertical.Items.Add(errorItem);
        ErrorCount++;
      }

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
      rightEditor.TranslationFileName.Text = string.IsNullOrEmpty(textEditorUI.TextEditorModel.FileName) ?
        Path.GetFileName(textEditorUI.TextEditorModel.FilePath) : textEditorUI.TextEditorModel.FileName; ;
      var fileName = textEditorUI.TextEditorModel.FileName;
      var filePath = textEditorUI.TextEditorModel.FilePath;
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

      var ok = models[0];
      if (ok.Mnemonic != "ОК")
      {
        return;
      }

      ProtocolUI.Header = (ok as OkCommandModel).ObjectCode;
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

    private void AddError(ErrorItem errorItem)
    {
      Application.Current.Dispatcher?.Invoke(() =>
      {
        ErrorListBoxVertical.Items.Add(errorItem);
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
        ErrorListBoxVertical.Items.Clear();
        ErrorCount = 0;
      });
    }

    private void TranslatorEditor_BackRequested(object? sender, EventArgs e)
    {
      var textEditorContainer = LeftBox.Children.Count > 0 ? LeftBox.Children[0] as TextEditorContainer : null;
      TranslatorNavigationService.TryOpenSourceFileFromTranslator(
        textEditorContainer,
        onSourceOpened: () => EditorEventAdapter.RaiseCloseRunItem(this));
    }

    private void BottomSplitter_OnDragStarted(object sender, DragStartedEventArgs e)
    {
      _userResizing = true;

      BottomRow.Height = new GridLength(ErrorListBoxVertical.ActualHeight);
      ErrorListBoxVertical.MaxHeight = double.PositiveInfinity;
    }

    private void BottomSplitter_OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
      _userResizing = false;
    }

    private void ErrorListBoxVertical_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
      if (_userResizing)
        return;

      double desired = ErrorListBoxVertical.ActualHeight;

      if (desired > MaxAutoHeight)
        desired = MaxAutoHeight;

      BottomRow.Height = GridLength.Auto;
      ErrorListBoxVertical.MaxHeight = desired;
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
        int line1 = doc.GetLineByOffset(anchors[i].Offset).LineNumber; // 1-based
        bool enabled = editor.IsBreakpointEnabled(cmd);

        ErrorListBoxVertical.UpsertBreakpoint(cmd, line1, enabled);
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
        editor.GoToLine(bp.RightLine.Value);
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
      ErrorListBoxVertical.UpsertBreakpoint(e.CommandNumber, line1, isEnabled: true);

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
      ErrorListBoxVertical.UpsertBreakpoint(e.CommandNumber, line1, isEnabled: true);

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
      ErrorListBoxVertical.UpsertBreakpoint(e.CommandNumber, line1, isEnabled: false);

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