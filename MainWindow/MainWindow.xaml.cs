using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AppConfiguration.SystemState;
using ConsoleUtilities;
using ConsoleUtilities.Engine;
using ConsoleUtilities.Services;
using MainWindowProgram.Engine;
using MainWindowProgram.Events;
using MainWindowProgram.Services;
using MainWindowProgram.ViewModels;
using UI.Controls.Search;
using Utilities.USB;
using static Utilities.LoggerUtility;

namespace MainWindowProgram
{
  public partial class MainWindow : Window
  {

    #region Поля.

    /// <summary>
    /// Обработчик сообщений, передаёт текст в интерфейс через связанный блок информации.
    /// Используется для отображения логов, статусов, ошибок и другой информации в UI.
    /// </summary>
    internal readonly MessageHandler messageHandler = new(_infoBlock);

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
    /// Класс для подписки на события жизненного цикла приложения (загрузка, закрытие и т.п.).
    /// Позволяет централизованно обрабатывать глобальные события приложения.
    /// </summary>
    private ApplicationEventsBinder _applicationEvents;

    #endregion

    /// <summary>
    /// Инициализирует новое окно приложения и настраивает события, командную строку, мониторинг USB и конфигурацию.
    /// </summary>
    public MainWindow()
    {
      InitializeComponent();
      this.Visibility = Visibility.Hidden;

      (var vm, var usb) = AppServices.Build(this);
      _viewModel = vm;
      _usbServices = usb;

      this.DataContext = _viewModel;
    }

    /// <summary>
    /// Запускает асинхронную инициализацию окна.
    /// </summary>
    public async Task InitializeAsync()
    {
      var lifecycle = new ApplicationLifecycleManager();
      lifecycle.Initialize(this, _usbServices, App._consoleManager);

      new CommandLineParser(_usbServices).ProcessCommandLineArgs();

      try
      {
        await Task.Run(() => Initialize());
      }
      catch (InvalidOperationException ex)
      {
        MessageBox.Show($"Ошибка темы: {ex.Message}");
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка приложения: {ex.Message}");
      }

      GuiInitializer.Apply(this);
      _searchWindow = new SearchWindow();
    }
  }
}
