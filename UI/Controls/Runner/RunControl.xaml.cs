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
using Message;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using UI.Controls.ErrorList;
using UI.Controls.ProtocolNew;
using UI.Controls.TextEditor;
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
    public string HeaderFile
    {
      get
      {
        return headerFile.Text;
      }
      set
      {
        headerFile.Text = value;
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
        if (newValue)
        {
          BackToFileButton.Visibility = Visibility.Collapsed;
          isLocked = true;
        }
        else
        {
          BackToFileButton.Visibility = Visibility.Visible;
          isLocked = false;
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

      var fileName = textEditorUI.TextEditorModel.FileName;
      var filePath = textEditorUI.TextEditorModel.FilePath;
      var dockItemPk = new DockItem
      {
        Title = fileName,
        TabText = fileName,
        Content = textEditorUI,
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
        var dockItem = dockManager.DockItems.FirstOrDefault(di => di.Content is TextEditorUI);
        if (dockItem != null)
        {
          editor = dockItem.Content as TextEditorUI;
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

    private void ArrowButton_Click(object sender, RoutedEventArgs e)
    {
      if (BackToFileButton.Visibility == Visibility.Visible)
      {
        var test = this.LeftBox.Children[0];
        if (test != null && test is TextEditorContainer textEditorContainer)
        {
          var foundItem = textEditorContainer.DockManager.DockItems.FirstOrDefault(item => item.Title != "Состояние оборудования");
          if (foundItem != null && foundItem.Content is TextEditorUI textEditor)
          {
            if (textEditor.TextEditorModel != null)
            {
              if (!string.IsNullOrEmpty(textEditor.TextEditorModel.FilePath)
                && File.Exists(textEditor.TextEditorModel.FilePath))
              {
                FileInteractionEventAdapter.RaiseOpenFileInEditorAgain(textEditor.TextEditorModel.FilePath);
                EditorEventAdapter.RaiseCloseRunItem(this);
              }
              else
              {
                MessageBoxCustom.Show("Ошибка обнаружения исходного файла", "Ошибка открытия файла", MessageBoxButton.OK, MessageBoxImage.Warning);
              }
            }
            else
            {
              MessageBoxCustom.Show("Текстовый редактор не найден", "Ошибка открытия файла", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
          }
          else
          {
            MessageBoxCustom.Show("Ошибка обнаружения исходного файла", "Ошибка открытия файла", MessageBoxButton.OK, MessageBoxImage.Warning);
          }
        }
        else
        {
          MessageBoxCustom.Show("Ошибка обнаружения исходного файла", "Ошибка открытия файла", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
      }
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

    private void BreakpointItemDoubleClicked(BreakpointListItem bp)
    {
      var editor = ChildTextEditorContainer.GetTextEditor();
      if (editor == null)
        return;

      if (bp.RightLine.HasValue && bp.RightLine.Value > 0)
        editor.GoToLine(bp.RightLine.Value);
    }

    private void BreakpointEnabledChanged(BreakpointListItem bp, bool enabled)
    {
      var editor = ChildTextEditorContainer.GetTextEditor();
      if (editor == null)
        return;

      if (!editor.HasBreakpointCommand(bp.CommandNumber))
        return;

      if (editor.IsBreakpointEnabled(bp.CommandNumber) == enabled)
        return;

      // Поднимаем события: On/Off обработчики обновят вкладку и модель
      if (enabled)
        editor.EnableBreakpoint(bp.CommandNumber, raiseEvents: true);
      else
        editor.DisableBreakpoint(bp.CommandNumber, raiseEvents: true);
    }

    private void OnBreakpointSet(BreakpointEvents.BreakpointSet e)
    {
      var editor = ChildTextEditorContainer.GetTextEditor();
      if (editor == null)
        return;

      int line1 = GetLine1BasedByCommand(editor, e.CommandNumber);
      ErrorListBoxVertical.UpsertBreakpoint(e.CommandNumber, line1, isEnabled: true);

      SetModelBreakpoint(e.CommandNumber, has: true, enabled: true);
    }

    private void OnBreakpointRemoved(BreakpointEvents.BreakpointRemoved e)
    {
      ErrorListBoxVertical.RemoveBreakpoint(e.CommandNumber);
      SetModelBreakpoint(e.CommandNumber, has: false, enabled: true);
    }

    private void OnBreakpointOn(BreakpointEvents.BreakpointOn e)
    {
      var editor = ChildTextEditorContainer.GetTextEditor();
      if (editor == null)
        return;

      int line1 = GetLine1BasedByCommand(editor, e.CommandNumber);
      ErrorListBoxVertical.UpsertBreakpoint(e.CommandNumber, line1, isEnabled: true);

      SetModelBreakpoint(e.CommandNumber, has: true, enabled: true);
    }

    private void OnBreakpointOff(BreakpointEvents.BreakpointOff e)
    {
      var editor = ChildTextEditorContainer.GetTextEditor();
      if (editor == null)
        return;

      int line1 = GetLine1BasedByCommand(editor, e.CommandNumber);
      ErrorListBoxVertical.UpsertBreakpoint(e.CommandNumber, line1, isEnabled: false);

      SetModelBreakpoint(e.CommandNumber, has: true, enabled: false);
    }

    private static int GetLine1BasedByCommand(TextEditorUI editor, int commandNumber)
    {
      var doc = editor.Document;
      int idx = editor.BreakpointCommandsNumbers.IndexOf(commandNumber);
      return doc.GetLineByOffset(editor.BreakPointLines[idx].Offset).LineNumber; // 1-based
    }

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