using Ask.Core.Services.EventCore.Adapters;
using MainWindowProgram.Engine;
using MainWindowProgram.HotkeyBindings;
using MainWindowProgram.Services;
using MainWindowProgram.ViewModels;
using Message;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using UI.Controls.Search;
using static Ask.LogLib.LoggerUtility;


namespace MainWindowProgram
{
  public partial class MainWindow : Window
  {
    #region Поля.

    /// <summary>
    /// Обработчик сообщений, передаёт текст в интерфейс через связанный блок информации.
    /// Используется для отображения логов, статусов, ошибок и другой информации в UI.
    /// </summary>
    internal MessageHandler messageHandler { get; set; }

    /// <summary>
    /// Статический UI-элемент, в который выводятся сообщения от системы.
    /// Используется как главный информационный блок в окне приложения.
    /// </summary>
    internal static TextBlock _infoBlock;

    /// <summary>
    /// Флаг, указывающий, заблокировано ли текущее состояние интерфейса.
    /// Используется для предотвращения повторных операций, если процесс уже запущен.
    /// </summary>
    internal bool IsLocked = false;

    /// <summary>
    /// Сервис управления USB-устройствами.
    /// Обеспечивает обнаружение, мониторинг и реакцию на USB-события.
    /// </summary>
    private readonly UsbServices _usbServices;

    /// <summary>
    /// ViewModel главного окна, содержащая команды, свойства и логику привязки данных.
    /// Связывает интерфейс с бизнес-логикой.
    /// </summary>
    private readonly MainWindowViewModel _viewModel;

    /// <summary>
    /// Показывает, активен ли в данный момент текстовый редактор.
    /// Значение true означает, что текстовый редактор находится в фокусе или используется пользователем.
    /// </summary>
    public bool IsTextEditorActive { get; set; }

    /// <summary>
    /// Сервис управления многооконным интерфейсом.
    /// </summary>
    public SearchWindow SearchWindow;

    private readonly TextEditorStatusViewModel _statusBarViewModel = new();

    #endregion

    /// <summary>
    /// Инициализирует новое окно приложения и настраивает события, командную строку, мониторинг USB и конфигурацию.
    /// </summary>
    public MainWindow()
    {
      InitializeComponent();

      this.Visibility = Visibility.Hidden;
      this.PreviewKeyDown += MainWindow_PreviewKeyDown;
      InputManager.Current.PreProcessInput += InputManager_PreProcessInput;

      (var vm, var usb) = AppServices.Build(this);
      _viewModel = vm;
      _usbServices = usb;

      StatusBar.DataContext = _statusBarViewModel;
      _statusBarViewModel.GetActiveEditor = () => MultiWindow.GetActiveTextEditor();

      this.DataContext = _viewModel;
      GuiInitializer.Apply(this);
    }

    /// <summary>
    /// Запускает асинхронную инициализацию окна.
    /// </summary>
    public async Task InitializeAsync()
    {
      var lifecycle = new ApplicationLifecycleManager();
      lifecycle.Initialize(this, _usbServices, _statusBarViewModel);

      new CommandLineParser(_usbServices).ProcessCommandLineArgs();
      ApplicationInitializer applicationInitializer = new ApplicationInitializer(messageHandler = new(_infoBlock));
      SystemStateEventAdapter.RaiseControlProgramActiveChanged(false);

      try
      {
        applicationInitializer.SubscribeToMessageEvents();

        await this.Dispatcher.InvokeAsync(() =>
        {
          HotkeyBinderManager.AttachAllHotkeys(this, this.DataContext);
        }, DispatcherPriority.Loaded);

      }
      catch (InvalidOperationException exception)
      {
        LogException($"Ошибка загрузки темы программы", exception);
        MessageBoxCustom.Show($"Ошибка загрузки темы: {exception.Message}", image: MessageBoxImage.Error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка выполнения программы", ex);
        MessageBoxCustom.Show($"Ошибка: {ex.Message}", image: MessageBoxImage.Error);
      }
    }

    public WindowState WindowStateStatus
    {
      get => this.WindowState;
    }

    private void ErrorMenuItem_Click(object sender, RoutedEventArgs e)
    {

    }

    /// <summary>
    /// Глобальные хоткеи для навигации по результатам поиска (F3/Shift+F3), даже когда окно SearchWindow не в фокусе.
    /// </summary>
    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape && SearchWindow != null && SearchWindow.IsVisible)
      {
        SearchWindow.CloseDialog();
        e.Handled = true;
        return;
      }

      if (e.Key != Key.F3 || Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Alt)
      {
        return;
      }

      if (SearchWindow == null || !SearchWindow.IsVisible)
      {
        return;
      }

      var direction = Keyboard.Modifiers == ModifierKeys.Shift ? "FindPrevious" : "FindNext";
      SearchEventAdapter.RaiseSearchButtonPressed(direction);
      e.Handled = true;
    }

    /// <summary>
    /// Глобальный перехват Esc/F3 вне зависимости от того, где фокус (включая окно поиска).
    /// Работает через InputManager, поэтому охватывает все окна приложения.
    /// </summary>
    private void InputManager_PreProcessInput(object? sender, PreProcessInputEventArgs e)
    {
      if (SearchWindow == null || !SearchWindow.IsVisible)
      {
        return;
      }

      if (e.StagingItem.Input is not KeyEventArgs ke || ke.RoutedEvent != Keyboard.KeyDownEvent)
      {
        return;
      }

      // Ctrl/Alt комбинации пропускаем — ими заведуют другие биндинги.
      if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) != 0)
      {
        return;
      }

      if (ke.Key == Key.Escape)
      {
        SearchWindow.CloseDialog();
        ke.Handled = true;
        return;
      }

      if (ke.Key == Key.F3)
      {
        var direction = Keyboard.Modifiers == ModifierKeys.Shift ? "FindPrevious" : "FindNext";
        SearchEventAdapter.RaiseSearchButtonPressed(direction);
        ke.Handled = true;
      }
    }
  }
}
