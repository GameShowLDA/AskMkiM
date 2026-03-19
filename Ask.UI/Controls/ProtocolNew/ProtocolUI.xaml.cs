using Ask.Core.Services.App;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Services.FilesUtility;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Metadata.Enums.HotkeysEnums;
using Ask.Core.Shared.Metadata.Static;
using Ask.UI.Infrastructure.UI.Overlay.Drawer.Runtime;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Controls.ProtocolNew
{
  /// <summary>
  /// Класс управления пользовательским интерфейсом протокола выполнения.
  /// Обеспечивает взаимодействие с пользователем, управление процессами и обработку сообщений.
  /// </summary>
  public partial class ProtocolUI : UserControl, ITextAdapter
  {
    static public event Action<object, KeyEventArgs> AnotherKeyPressed;
    private bool loaded = false;
    public event EventHandler? OpenFileRequested;
    public event EventHandler? OpenFolderRequested;
    public event EventHandler? PrintRequested;

    /// <summary>
    /// Свойство зависимости для заголовка.
    /// Позволяет изменять заголовок через XAML или код.
    /// </summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(ProtocolUI),
            new PropertyMetadata(string.Empty, OnHeaderChanged));

    /// <summary>
    /// Получает или задает текст заголовка.
    /// </summary>
    public string Header
    {
      get => (string)GetValue(HeaderProperty); 
      set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Обработчик изменения заголовка, чтобы обновлять TextBlock.
    /// </summary>
    private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is ProtocolUI control)
      {
        control.header.Text = e.NewValue as string;
        control.headerFile.Text = e.NewValue as string;
        control.FileName.Text = e.NewValue as string;
      }
    }

    /// <summary>
    /// Свойство зависимости для установки динамического контента.
    /// Позволяет добавить стандартный элемент (Button, TextBlock и т. д.) или UserControl.
    /// </summary>
    public static readonly DependencyProperty ContentViewProperty =
        DependencyProperty.Register(
            nameof(ContentView),
            typeof(FrameworkElement),
            typeof(ProtocolUI),
            new PropertyMetadata(null));

    /// <summary>
    /// Получает или задает контент (стандартный элемент или UserControl).
    /// </summary>
    public FrameworkElement ContentView
    {
      get => (FrameworkElement)GetValue(ContentViewProperty);
      set => SetValue(ContentViewProperty, value);
    }

    public static readonly DependencyProperty IsTopMenuVisibleProperty =
            DependencyProperty.Register(
                nameof(IsTopMenuVisible),
                typeof(bool),
                typeof(ProtocolUI),
                new PropertyMetadata(false));

    public bool IsTopMenuVisible
    {
      get => (bool)GetValue(IsTopMenuVisibleProperty);
      set => SetValue(IsTopMenuVisibleProperty, value);
    }

    public Visibility ErrorListBoxVerticalVisibility
    {
      get => ErrorListBoxVertical.Visibility;
      set
      {
        ErrorListBoxVertical.Visibility = value;
        SeparatorError.Visibility = value;
      }
    }

    /// <summary>
    /// Команда для установки динамического контента из XAML.
    /// </summary>
    public ICommand SetContentCommand { get; }

    /// <summary>
    /// Коллекция элементов, используемых в протоколе.
    /// </summary>
    public ObservableCollection<object> Items { get; }

    private Window _attachedWindow;

    private Action<ExecutionEvents.ControlButtonPressed> _controlButtonHandler;
    private Action<ExecutionEvents.StepByStepModeChanged> _stepByStepModeChangedHandler;
    private bool _eventSubscriptionsAttached;

    /// <summary>
    /// Конструктор по умолчанию для элемента ProtocolSelfCheck.
    /// Инициализирует компоненты и устанавливает обработчики событий PreviewMouseDown для кнопок.
    /// </summary>
    public ProtocolUI() : this(false)
    {
    }

    public ProtocolUI(bool isTopMenuVisible)
    {
      IsTopMenuVisible = isTopMenuVisible;
      Items = new ObservableCollection<object>();
      ActionExecutor = Task.Run(() => ActionExecutor.CreateInstanceAsync(this)).Result;
      _controlButtonHandler = OnControlButtonPressed;
      _stepByStepModeChangedHandler = e => EventAggregator_StepByStepModeChanged(e.IsEnabled);
      InitializeInternal();
      Loaded += OnLoaded;
      Unloaded += OnUnloaded;
    }

    private void EventAggregator_StepByStepModeChanged(bool obj)
    {
      UpdateStepButtonsForCurrentState(obj);
    }

    private void InitializeInternal()
    {
      if (loaded)
        return;

      loaded = true;
      InitializeComponent();
      this.DataContext = this;

      loopButton.Visibility = Visibility.Collapsed;
      RepeatButtonElement.Visibility = Visibility.Collapsed;

      SetupButtons();

      this.Loaded += (s, e) =>
      {
        AttachEventSubscriptions();

        ErrorListBoxVertical.ItemDoubleClicked -= ErrorListBoxVertical_ErrorItemDoubleClicked;
        ErrorListBoxVertical.ItemDoubleClicked += ErrorListBoxVertical_ErrorItemDoubleClicked;
        _attachedWindow = Application.Current?.MainWindow;
        if (_attachedWindow != null)
        {
          Keyboard.AddKeyDownHandler(_attachedWindow, OnGlobalKeyDown);
        }

        KeyboardManager.RegisterGlobalStepHooks();
        RegisterHotkeys();
      };

      this.Unloaded += (s, e) =>
      {
        if (_attachedWindow != null)
        {
          Keyboard.RemoveKeyDownHandler(_attachedWindow, OnGlobalKeyDown);
        }

        KeyboardManager.UnregisterGlobalStepHooks();
      };

      ButtonService = this;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      AttachEventSubscriptions();
    }

    private void AttachEventSubscriptions()
    {
      if (_eventSubscriptionsAttached)
      {
        return;
      }

      EventAggregator.Subscribe(_controlButtonHandler);
      EventAggregator.Subscribe(_stepByStepModeChangedHandler);
      _eventSubscriptionsAttached = true;
    }

    private void OnGlobalKeyDown(object sender, KeyEventArgs e)
    {
      var key = e.Key == Key.System ? e.SystemKey : e.Key;
      var modifiers = Keyboard.Modifiers;
      if (DrawerHostService.Instance.ShouldBlockGlobalInput)
      {
        return;
      }

      if (Keyboard.FocusedElement is TextBox or PasswordBox or ComboBox)
        return;

      switch (key)
      {
        case Key.Enter:
          if (modifiers == ModifierKeys.None && StartButtonElement.Visibility == Visibility.Visible)
          {
            KeyboardManager.OnStartPressed?.Invoke();
            e.Handled = true;
          }
          break;

        case Key.F5:
          if (modifiers == ModifierKeys.None)
          {
            HandleRunOrPause();
            e.Handled = true;
          }
          break;

        case Key.F10:
          if (modifiers == ModifierKeys.None)
          {
            HandleStepModeStart(isStepInto: false);
            e.Handled = true;
          }
          break;

        case Key.F11:
          if (modifiers == ModifierKeys.None)
          {
            HandleStepModeStart(isStepInto: true);
            e.Handled = true;
          }
          break;

        case Key.P:
          if (modifiers == ModifierKeys.None && ContinueButtonElement.Visibility == Visibility.Visible)
          {
            KeyboardManager.OnContinuePressed?.Invoke();
          }
          else if (modifiers == ModifierKeys.None && PauseButtonElement.Visibility == Visibility.Visible)
          {
            KeyboardManager.OnPausePressed?.Invoke();
          }
          if (modifiers == ModifierKeys.None)
          {
            e.Handled = true;
          }
          break;

        case Key.Escape:
          if (modifiers == ModifierKeys.None &&
              (StopButtonElement.Visibility == Visibility.Visible
              || ContinueButtonElement.Visibility == Visibility.Visible
              || PauseButtonElement.Visibility == Visibility.Visible))
          {
            KeyboardManager.OnExitPressed?.Invoke();
            e.Handled = true;
          }
          break;

        case Key.R:
          if (modifiers == ModifierKeys.None && RepeatButtonElement.Visibility == Visibility.Visible)
          {
            KeyboardManager.OnRepeatPressed?.Invoke();
            e.Handled = true;
          }
          break;
        default:
          var focus = Keyboard.FocusedElement;
          if (key == Key.LeftAlt || key == Key.RightAlt)
          {
            AnotherKeyPressed?.Invoke(sender, e);
          }
          break;
      }
    }

    private void stepOverButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      StepControlManager.RequestStepOverUntilNextControlCommand();
      KeyboardManager.TriggerStep();
    }

    private void stepIntoButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      StepControlManager.SetStepIntoMode();
      KeyboardManager.TriggerStep();
    }

    public string GetText()
    {
      return protocolTextBox.GetText();
    }

    public void MenuButtonVisibility(bool visibility)
    {
      MenuButton.Visibility = visibility ? Visibility.Visible : Visibility.Collapsed;
    }

    public ObservableCollection<ShowMessageModel> GetShowMessageModels()
    {
      return protocolTextBox.GetShowMessageModels();
    }

    private void ArrowButton_Click(object sender, RoutedEventArgs e)
    {
      var vis = ArrowButton.IsArrowUp ? Visibility.Visible : Visibility.Collapsed;

      header.Visibility = vis;
      ContentPanel.Visibility = vis;
      BigButtonsPanel.Visibility = vis;
    }

    private void OnControlButtonPressed(ExecutionEvents.ControlButtonPressed e)
    {
      // Эти события публикуются только в breakpoint-flow.
      // Здесь нельзя повторно звать обычные обработчики F5/F10/F11,
      // потому что они снова публикуют те же ControlButtonPressed
      // и приводят к рекурсии/ложной паузе.
      switch (e.Button)
      {
        case ExecutionControlButton.Run:
          ShowOnlyStopAndFinishButtons(false);
          break;

        case ExecutionControlButton.StepOver:
          ShowOnlyStopAndFinishButtons(false);
          break;

        case ExecutionControlButton.StepInto:
          ShowOnlyStopAndFinishButtons(true);
          break;
      }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
      if (!_eventSubscriptionsAttached)
      {
        return;
      }

      EventAggregator.Unsubscribe(_controlButtonHandler);
      EventAggregator.Unsubscribe(_stepByStepModeChangedHandler);
      _eventSubscriptionsAttached = false;
    }

    /// <summary>
    /// Обрабатывает запуск, продолжение или паузу выполнения
    /// в зависимости от текущего состояния UI.
    /// </summary>
    private void HandleRunOrPause()
    {
      if (StartButtonElement.Visibility == Visibility.Visible)
      {
        KeyboardManager.OnStartPressed?.Invoke();
        return;
      }

      // Приоритет у текущего отображаемого состояния:
      // если видна "Продолжить" — продолжаем,
      // если видна "Пауза" — ставим на паузу (в т.ч. во время F10-run).
      if (ContinueButtonElement.Visibility == Visibility.Visible)
      {
        KeyboardManager.OnContinuePressed?.Invoke();
      }
      else if (PauseButtonElement.Visibility == Visibility.Visible)
      {
        KeyboardManager.OnPausePressed?.Invoke();
      }
    }

    /// <summary>
    /// Обрабатывает запуск выполнения в пошаговом режиме
    /// (F10 / F11), если доступен старт.
    /// </summary>
    private void HandleStepModeStart(bool isStepInto)
    {
      if (StartButtonElement.Visibility == Visibility.Visible)
      {
        KeyboardManager.OnStartPressedByStepMode?.Invoke();
        return;
      }

      if (ContinueButtonElement.Visibility == Visibility.Visible)
      {
        if (isStepInto)
        {
          BottomLayer_PreviewMouseDown(StepIntoButtonElement, CreateMouseArgs());
        }
        else
        {
          TopLayer_PreviewMouseDown(StepOverButtonElement, CreateMouseArgs());
        }
      }
    }

    private void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
      if (OpenFileRequested != null)
      {
        OpenFileRequested.Invoke(this, EventArgs.Empty);
        return;
      }

      OpenLatestProtocolInEditor();
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
      if (OpenFolderRequested != null)
      {
        OpenFolderRequested.Invoke(this, EventArgs.Empty);
        return;
      }

      OpenLatestProtocolFolder();
    }

    private void PrintButton_Click(object sender, RoutedEventArgs e)
    {
      if (PrintRequested != null)
      {
        PrintRequested.Invoke(this, EventArgs.Empty);
        return;
      }

      try
      {
        PrintUtility.PrintProtocol(GetShowMessageModels());
      }
      catch (Exception ex)
      {
        LogException("Ошибка печати протокола", ex);
      }
    }

    private void OpenLatestProtocolInEditor()
    {
      try
      {
        var latestProtocolPath = ResolveLatestProtocolPath();
        if (string.IsNullOrWhiteSpace(latestProtocolPath))
        {
          return;
        }

        FileInteractionEventAdapter.RaiseOpenFileInEditorAgain(latestProtocolPath);
      }
      catch (Exception ex)
      {
        LogException("Ошибка при открытии протокола в редакторе", ex);
      }
    }

    private void OpenLatestProtocolFolder()
    {
      try
      {
        var latestProtocolPath = ResolveLatestProtocolPath();
        if (!string.IsNullOrWhiteSpace(latestProtocolPath) && File.Exists(latestProtocolPath))
        {
          Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{Path.GetFullPath(latestProtocolPath)}\"")
          {
            UseShellExecute = true
          });
          return;
        }

        var historyDirectory = Path.GetFullPath(Path.Combine("..", FileLocations.DataSaveDirectory));
        Directory.CreateDirectory(historyDirectory);

        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{historyDirectory}\"")
        {
          UseShellExecute = true
        });
      }
      catch (Exception ex)
      {
        LogException("Ошибка при открытии папки протоколов", ex);
      }
    }

    private string? ResolveLatestProtocolPath()
    {
      if (!string.IsNullOrWhiteSpace(_lastSavedProtocolPath) && File.Exists(_lastSavedProtocolPath))
      {
        return Path.GetFullPath(_lastSavedProtocolPath);
      }

      var historyDirectory = Path.GetFullPath(Path.Combine("..", FileLocations.DataSaveDirectory));
      if (!Directory.Exists(historyDirectory))
      {
        return null;
      }

      return Directory
        .EnumerateFiles(historyDirectory, "*.lstw", SearchOption.AllDirectories)
        .OrderByDescending(File.GetLastWriteTimeUtc)
        .FirstOrDefault();
    }
  }
}

