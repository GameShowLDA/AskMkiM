using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Static;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using Message;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using UI.Components.Invoke;
using UI.Components.MultiEditorMethods;
using UI.Controls.ProtocolNew;
using UI.Controls.TextEditor;
using UI.Services;
using UI.Windows.WpfDocking.Windows.Docking;
using static Ask.LogLib.LoggerUtility;

namespace UI.Controls.Runner
{
  /// <summary>
  /// Логика взаимодействия для RunControl.xaml
  /// </summary>
  public partial class RunControl : UserControl
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

    private bool task = false;
    public RunControl()
    {
      InitializeComponent();
      ProtocolUI = new ProtocolUI(true);
      ProtocolUI.ErrorListBoxVerticalVisibility = Visibility.Collapsed;
      MainContent.Content = ProtocolUI;
      ErrorListBoxVertical.ItemDoubleClicked += ErrorItemDoubleClicked;
      EventAggregator.Subscribe<SystemStateEvents.LockedChanged>(e => OnLockedChanged(e.IsLocked));


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
          //_leftEditor?.GoToLine(obj.FormattedLineNumber);
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
      // Отменяем фокусировку и возвращаем в MainContent
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
      //LeftBox.Children.Clear();
      //LeftBox.Children.Add(textEditorUI);
      if (dockManager.DockItems.Count > 0)
      {
        foreach (var dockItem in dockManager.DockItems.ToList())
        {
          dockManager.DockItems.Remove(dockItem);
        }
      }

      var fileName = Path.GetFileName(textEditorUI.TextEditorModel.FilePath);
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
        Content = new TextEditorUI(),
      };

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

            SystemStateManager.SetIsControlProgramActive(isControlProgramActive).ConfigureAwait(true);
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

        SystemStateManager.SetIsControlProgramActive(isControlProgramActive).ConfigureAwait(true);
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
        if (test != null && test is TextEditorUI textEditor)
        {
          if (textEditor.TextEditorModel != null
            && !string.IsNullOrEmpty(textEditor.TextEditorModel.FilePath)
            && File.Exists(textEditor.TextEditorModel.FilePath))
          {
            FileInteractionEventAdapter.RaiseOpenFileInEditorAgain(textEditor.TextEditorModel.FilePath);
            EditorEventAdapter.RaiseCloseRunItem(this);
          }
        }
        else
        {
          MessageBoxCustom.Show("Ошибка обнаружения исходного файла", "Ошибка открытия файла", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
      }
    }

    // Пользователь начал тянуть сплиттер – не вмешиваемся
    private void BottomSplitter_OnDragStarted(object sender, DragStartedEventArgs e)
    {
      _userResizing = true;

      // Переводим строку из Auto → FixedHeight,
      // чтобы пользователь мог растягивать вручную
      BottomRow.Height = new GridLength(ErrorListBoxVertical.ActualHeight);
      ErrorListBoxVertical.MaxHeight = double.PositiveInfinity;
    }

    // Закончил тянуть – теперь снова можем автоподстраивать при изменении контента
    private void BottomSplitter_OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
      _userResizing = false;
    }

    // Панель ошибок изменила размер (добавились/убрались строки)
    private void ErrorListBoxVertical_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
      if (_userResizing)
        return;

      double desired = ErrorListBoxVertical.ActualHeight;

      if (desired > MaxAutoHeight)
        desired = MaxAutoHeight;

      // Автоматический режим — строка остаётся Auto, но мы ограничиваем контент
      BottomRow.Height = GridLength.Auto;
      ErrorListBoxVertical.MaxHeight = desired;
    }
  }
}
