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
using Ask.UI.Controls.ProtocolNew;
using UI.Controls.TextEditor;
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

    private const double MaxAutoHeight = 250.0;

    public int ErrorCount { get; private set; } = 0;

    private ProtocolUI ProtocolUI { get; set; }

    public string FileName { get; set; }

    public string OpkFilePath { get; set; }

    private List<BaseCommandModel> translationModels = new List<BaseCommandModel>();

    private TextEditorContainer _leftEditor;
    private readonly ArchiveSaveService _archiveSaveService = new ArchiveSaveService();
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
      if (errorItems == null || errorItems.Count == 0)
        return;

      ErrorListBoxVertical.AddErrors(errorItems);
      ErrorCount += errorItems.Count;

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
      var fileName = textEditorUI.TextEditorModel.FileName;
      var filePath = textEditorUI.TextEditorModel.FilePath;
      rightEditor.TranslationFileName.Text = string.IsNullOrEmpty(fileName) ?
        Path.GetFileName(filePath) : fileName;
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

      if (dockManager == null)
      {
        LogError("ChildTextEditorContainer.DockControl не найден (null). Невозможно отобразить вкладку.");
        return;
      }

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

      _archiveSaveService.SaveFileToArchive(this, this.TranslationModels, sourceFilePath);
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
  }
}

