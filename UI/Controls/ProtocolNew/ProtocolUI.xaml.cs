using Ask.Core.Services.App;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Metadata.Enums.HotkeysEnums;
using Ask.UI.Infrastructure.UI.Overlay.Drawer.Runtime;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Controls.ProtocolNew
{
  /// <summary>
  /// Класс управления пользовательским интерфейсом протокола выполнения.
  /// Обеспечивает взаимодействие с пользователем, управление процессами и обработку сообщений.
  /// </summary>
  public partial class ProtocolUI : UserControl, ITextAdapter
  {
    static public event Action<object, KeyEventArgs> AnotherKeyPressed;
    private bool loaded = false;

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

    /// <summary>
    /// Конструктор по умолчанию для элемента ProtocolSelfCheck.
    /// Инициализирует компоненты и устанавливает обработчики событий PreviewMouseDown для кнопок.
    /// </summary>
    public ProtocolUI() : this(false)
    {
      _controlButtonHandler = OnControlButtonPressed;
      EventAggregator.Subscribe(_controlButtonHandler);
      Unloaded += OnUnloaded;
    }

    public ProtocolUI(bool isTopMenuVisible)
    {
      IsTopMenuVisible = isTopMenuVisible;
      Items = new ObservableCollection<object>();
      ActionExecutor = Task.Run(() => ActionExecutor.CreateInstanceAsync(this)).Result;
      InitializeInternal();
      EventAggregator.Subscribe<ExecutionEvents.StepByStepModeChanged>(e => EventAggregator_StepByStepModeChanged(e.IsEnabled));
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
      switch (e.Button)
      {
        case ExecutionControlButton.Run:
          HandleRunOrPause();
          break;

        case ExecutionControlButton.StepOver:
          HandleStepModeStart(isStepInto: false);
          break;

        case ExecutionControlButton.StepInto:
          HandleStepModeStart(isStepInto: true);
          break;
      }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
      EventAggregator.Unsubscribe(_controlButtonHandler);
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
  }
}
