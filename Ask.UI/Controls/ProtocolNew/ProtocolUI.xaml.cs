using Ask.Core.Services.App;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Services.FilesUtility;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Enums.HotkeysEnums;
using Ask.UI.Infrastructure.UI.Overlay.Drawer.Runtime;
using Ask.UI.Features.ProtocolNew.Execution;
using Ask.UI.Features.ProtocolNew.Hotkeys;
using Ask.UI.Features.ProtocolNew.Protocol;
using Ask.Engine.ControlCommandExecutor.Execution;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
  public partial class ProtocolUI : UserControl, ITextAdapter, IProtocolHotkeyContext, IExecutionCommandJumpGate
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

    /// <summary>
    /// Контроллер маршрутизации горячих клавиш в команды исполнительного интерфейса.
    /// </summary>
    private ProtocolHotkeyController _hotkeyController = null!;

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
      ActionExecutor = ActionExecutor.CreateInstance(this);
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
      _protocolStorage = new ProtocolStorageService();
      _inspectionProtocolAreaController = new InspectionProtocolAreaController(_protocolStorage, this);
      inspectionProtocolTextBox.SetFileType(FileType.InspectionProtocol);
      inspectionProtocolTextBox.WordWrap = true;
      ClearInspectionProtocol();
      this.DataContext = this;

      loopButton.Visibility = Visibility.Collapsed;
      RepeatButtonElement.Visibility = Visibility.Collapsed;

      SetupButtons();
      _hotkeyController = new ProtocolHotkeyController(this);
      _entryOutputService = new ProtocolEntryOutputService(this);
      _postOutputController = new ProtocolPostOutputController(this);

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
      _hotkeyController.HandleKeyDown(sender, e);
    }

    /// <inheritdoc />
    bool IProtocolHotkeyContext.CanStart => StartButtonElement.Visibility == Visibility.Visible;

    /// <inheritdoc />
    bool IProtocolHotkeyContext.CanPause => PauseButtonElement.Visibility == Visibility.Visible;

    /// <inheritdoc />
    bool IProtocolHotkeyContext.CanContinue => ContinueButtonElement.Visibility == Visibility.Visible;

    /// <inheritdoc />
    bool IProtocolHotkeyContext.CanExit =>
      StopButtonElement.Visibility == Visibility.Visible
      || ContinueButtonElement.Visibility == Visibility.Visible
      || PauseButtonElement.Visibility == Visibility.Visible;

    /// <inheritdoc />
    bool IProtocolHotkeyContext.CanRepeat => RepeatButtonElement.Visibility == Visibility.Visible;

    /// <inheritdoc />
    bool IProtocolHotkeyContext.CanJumpToCommand =>
      ContinueButtonElement.Visibility == Visibility.Visible;

    /// <inheritdoc />
    void IProtocolHotkeyContext.Start() => StartFromHotkey();

    /// <inheritdoc />
    void IProtocolHotkeyContext.RunOrPause() => HandleRunOrPause();

    /// <inheritdoc />
    void IProtocolHotkeyContext.Step(bool isStepInto) => HandleStepModeStart(isStepInto);

    /// <inheritdoc />
    void IProtocolHotkeyContext.Pause() => PauseFromHotkey();

    /// <inheritdoc />
    void IProtocolHotkeyContext.Continue() => ContinueFromHotkey();

    /// <inheritdoc />
    void IProtocolHotkeyContext.Exit() => ExitFromHotkey();

    /// <inheritdoc />
    void IProtocolHotkeyContext.Repeat() => RepeatFromHotkey();

    /// <inheritdoc />
    void IProtocolHotkeyContext.JumpToCommand() => RequestCommandJump();

    /// <inheritdoc />
    bool IExecutionCommandJumpGate.IsExecutionPaused => ActionExecutor.IsPaused;

    /// <inheritdoc />
    void IExecutionCommandJumpGate.InterruptPauseForCommandJump() =>
      ActionExecutor.InterruptPauseForCommandJump();

    /// <inheritdoc />
    void IProtocolHotkeyContext.NotifyOtherKey(object sender, KeyEventArgs e) =>
      AnotherKeyPressed?.Invoke(sender, e);

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

    public IReadOnlyList<ShowMessageModel> GetShowMessageModels()
    {
      return protocolTextBox.GetMessagesSnapshot();
    }

    private void ArrowButton_Click(object sender, RoutedEventArgs e)
    {
      var vis = ArrowButton.IsArrowUp ? Visibility.Visible : Visibility.Collapsed;

      header.Visibility = vis;
      ContentPanel.Visibility = vis;
      BigButtonsPanel.Visibility = vis;
    }

    private void CommandJumpButtonTop_Click(object sender, RoutedEventArgs e) => RequestCommandJump();

    private async void RequestCommandJump()
    {
      if (StepControlManager.IsBreakpointStepModeActive && StepControlManager.BreakpointCommandInfo != null)
      {
        ExecutionEventAdapter.RaiseBreakpointF4Pressed(StepControlManager.BreakpointCommandInfo);
        return;
      }

      try
      {
        await CommandExecutionManager.RequestPausedCommandJumpAsync();
      }
      catch (Exception exception)
      {
        LogException("Ошибка перехода к выбранной команде", exception);
      }
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
        StartFromHotkey();
        return;
      }

      // Приоритет у текущего отображаемого состояния:
      // если видна "Продолжить" — продолжаем,
      // если видна "Пауза" — ставим на паузу (в т.ч. во время F10-run).
      if (ContinueButtonElement.Visibility == Visibility.Visible)
      {
        ContinueFromHotkey();
      }
      else if (PauseButtonElement.Visibility == Visibility.Visible)
      {
        PauseFromHotkey();
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

        var historyDirectory = _protocolStorage.GetHistoryDirectory();
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
      return _protocolStorage.ResolveLatestExecutionProtocolPath();
    }
  }
}
