using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AppConfiguration.Interface;
using static Utilities.LoggerUtility;

namespace UI.Controls.ProtocolNew
{
  /// <summary>
  /// Класс управления пользовательским интерфейсом протокола выполнения.
  /// Обеспечивает взаимодействие с пользователем, управление процессами и обработку сообщений.
  /// </summary>
  public partial class ProtocolUI : UserControl, ITextAdapter
  {
    static public event Action<object, KeyEventArgs> AnotherKeyPressed;
    bool loaded = false;

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


      AppConfiguration.Services.UserMessageServiceProvider.Instance = this;

      SetupButtons();

      this.Loaded += (s, e) =>
      {
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
      LogInformation($"[KEYBOARD] (Keyboard.AddKeyDownHandler) Обнаружена клавиша: {key}");

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
          // TODO: закрытие обычных вкладок через alt+x
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
      return protocolTextBox.GetPlainTextAsync().Result;
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
  }
}
