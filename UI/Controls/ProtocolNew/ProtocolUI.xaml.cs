using AppConfiguration.Interface;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Utilities.LoggerUtility;

namespace UI.Controls.ProtocolNew
{
  /// <summary>
  /// Класс управления пользовательским интерфейсом протокола выполнения.
  /// Обеспечивает взаимодействие с пользователем, управление процессами и обработку сообщений.
  /// </summary>
  public partial class ProtocolUI : UserControl, ITextAdapter
  {
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

    /// <summary>
    /// Команда для установки динамического контента из XAML.
    /// </summary>
    public ICommand SetContentCommand { get; }

    /// <summary>
    /// Коллекция элементов, используемых в протоколе.
    /// </summary>
    public ObservableCollection<object> Items { get; }

    /// <summary>
    /// Конструктор по умолчанию для элемента ProtocolSelfCheck.
    /// Инициализирует компоненты и устанавливает обработчики событий PreviewMouseDown для кнопок.
    /// </summary>
    public ProtocolUI()
    {
      InitializeComponent();
      this.DataContext = this;

      loopButton.Visibility = Visibility.Collapsed;
      returnButton.Visibility = Visibility.Collapsed;
      Items = new ObservableCollection<object>();

      AppConfiguration.Services.UserMessageServiceProvider.Instance = this;

      SetupButtons();
      ActionExecutor = Task.Run(() => ActionExecutor.CreateInstanceAsync(this)).Result;

      this.Loaded += (s, e) =>
      {
        KeyboardManager.RegisterGlobalStepHooks();
        AttachKeyboardHandlers();
        RegisterHotkeys();
      };

      this.Unloaded += (s, e) =>
      {
        KeyboardManager.UnregisterGlobalStepHooks();
        DetachKeyboardHandlers();
      };
    }
    private void AttachKeyboardHandlers()
    {
      Keyboard.AddKeyDownHandler(Application.Current.MainWindow, OnGlobalKeyDown);
    }

    private void DetachKeyboardHandlers()
    {
      Keyboard.RemoveKeyDownHandler(Application.Current.MainWindow, OnGlobalKeyDown);
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
          if (StartButton.Visibility == Visibility.Visible)
          {
            KeyboardManager.OnStartPressed?.Invoke();
          }
          e.Handled = true;
          break;

        case Key.P:
          if (NextButtonVisibility == Visibility.Visible)
          {
            KeyboardManager.OnContinuePressed?.Invoke();
          }
          else if (PauseButton.Visibility == Visibility.Visible)
          {
            KeyboardManager.OnPausePressed?.Invoke();
          }
          e.Handled = true;
          break;

        case Key.Escape:
          if (ExitButton.Visibility == Visibility.Visible)
          {
            KeyboardManager.OnExitPressed?.Invoke();
          }
          e.Handled = true;
          break;

        case Key.R:
          if (ReturnMeasureResistanceButtonVisibility == Visibility.Visible)
          {
            KeyboardManager.OnRepeatPressed?.Invoke();
          }
          e.Handled = true;
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
  }
}
