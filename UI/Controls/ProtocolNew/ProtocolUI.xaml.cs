using Ask.Core.Services.App;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
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

    /// <summary>
    /// Конструктор по умолчанию для элемента ProtocolSelfCheck.
    /// Инициализирует компоненты и устанавливает обработчики событий PreviewMouseDown для кнопок.
    /// </summary>
    public ProtocolUI() : this(false) { }

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
      Application.Current.Dispatcher.Invoke(() =>
      {
        StepOver.Visibility = obj ? Visibility.Visible : Visibility.Collapsed;
        StepInto.Visibility = obj ? Visibility.Visible : Visibility.Collapsed;
        StepOverButtonElement.Visibility = obj ? Visibility.Visible : Visibility.Collapsed;
        StepIntoButtonElement.Visibility = obj ? Visibility.Visible : Visibility.Collapsed;
      });
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

      if (Keyboard.FocusedElement is TextBox or PasswordBox or ComboBox)
        return;

      switch (key)
      {
        case Key.Enter:
          if (StartButtonElement.Visibility == Visibility.Visible)
          {
            KeyboardManager.OnStartPressed?.Invoke();
          }
          e.Handled = true;
          break;

        case Key.F5:
          if (StartButtonElement.Visibility == Visibility.Visible)
          {
            KeyboardManager.OnStartPressed?.Invoke();
          }
          else if (ContinueButtonElement.Visibility == Visibility.Visible)
          {
            KeyboardManager.OnContinuePressed?.Invoke();
          }
          else if (PauseButtonElement.Visibility == Visibility.Visible)
          {
            KeyboardManager.OnPausePressed?.Invoke();
          }

          e.Handled = true;
          break;

        case Key.F10:
        case Key.F11:
          if (StartButtonElement.Visibility == Visibility.Visible)
          {
            KeyboardManager.OnStartPressedByStepMode?.Invoke();
          }
          e.Handled = true;
          break;

        case Key.P:
          if (ContinueButtonElement.Visibility == Visibility.Visible)
          {
            KeyboardManager.OnContinuePressed?.Invoke();
          }
          else if (PauseButtonElement.Visibility == Visibility.Visible)
          {
            KeyboardManager.OnPausePressed?.Invoke();
          }
          e.Handled = true;
          break;

        case Key.Escape:
          if (StopButtonElement.Visibility == Visibility.Visible)
          {
            KeyboardManager.OnExitPressed?.Invoke();
          }
          e.Handled = true;
          break;

        case Key.R:
          if (RepeatButtonElement.Visibility == Visibility.Visible)
          {
            KeyboardManager.OnRepeatPressed?.Invoke();
          }
          e.Handled = true;
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
      StepControlManager.IsStepInto = false;
      KeyboardManager.TriggerStep();
    }

    private void stepIntoButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      StepControlManager.IsStepInto = true;
      KeyboardManager.TriggerStep();
    }

    public string GetText()
    {
      return protocolTextBox.GetText();
    }

    /// <summary>
    /// Ожидает нажатия одной из двух административных кнопок.
    /// Возвращает true, если нажали ПРОПУСТИТЬ, false — если ЗАВЕРШИТЬ.
    /// </summary>
    public Task<bool> WaitAdminButtonAsync()
    {
      _adminButtonTcs = new TaskCompletionSource<bool>();

      adminContinue.Click += OnAdminContinueClicked;
      adminExit.Click += OnAdminExitClicked;

      SetupAdminButton();

      return _adminButtonTcs.Task;
    }

    private void OnAdminContinueClicked(object sender, RoutedEventArgs e)
    {
      CleanupAdminButtonHandlers();
      _adminButtonTcs?.TrySetResult(true);
    }

    private void OnAdminExitClicked(object sender, RoutedEventArgs e)
    {
      CleanupAdminButtonHandlers();
      _adminButtonTcs?.TrySetResult(false);
    }

    /// <summary>
    /// Снимает обработчики после клика (чтобы не было дублирования).
    /// </summary>
    private void CleanupAdminButtonHandlers()
    {
      adminContinue.Click -= OnAdminContinueClicked;
      adminExit.Click -= OnAdminExitClicked;
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

  }
}
