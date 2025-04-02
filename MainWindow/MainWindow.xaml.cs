using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AppConfiguration.SystemState;
using ConsoleUtilities;
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
    /// <summary>
    /// Таймер, используемый для периодических задач (если необходимо).
    /// </summary>
    static System.Timers.Timer timer = new System.Timers.Timer();

    /// <summary>
    /// Статический блок информации для отображения сообщений.
    /// </summary>
    static TextBlock _infoBlock;

    private bool isLocked = false;

    /// <summary>
    /// Обработчик сообщений, принимает блок информации для вывода.
    /// </summary>
    MessageHandler messageHandler = new MessageHandler(_infoBlock);

    UsbServices usbServices = new UsbServices();

    // Менеджер консоли (Singleton), отвечающий за переключение режима консоли и обработку событий администратора.
    private readonly ConsoleManager _consoleManager;

    private readonly MainWindowViewModel _viewModel;

    ApplicationEventsBinder ApplicationEvents;

    /// <summary>
    /// Инициализирует новое окно приложения и настраивает события, командную строку, мониторинг USB и конфигурацию.
    /// </summary>
    public MainWindow()
    {
      InitializeComponent();

      var multiWindowService = new MultiWindowService(this.MultiWindow);
      var metrologyService = new MetrologyService(multiWindowService);
      var fileService = new FileService(multiWindowService, () => isLocked);
      var testService = new TestService(multiWindowService);
      var settingsService = new SettingsService(multiWindowService);
      var adminService = new AdminServices(this, multiWindowService);
      var windowService = new WindowService(this, mainMenu, ButtonsPanel, () => isLocked);

      _viewModel = new MainWindowViewModel(metrologyService, fileService, testService, settingsService, adminService, windowService);
      this.DataContext = _viewModel;

      _consoleManager = ConsoleManager.Instance;
      this.Visibility = Visibility.Hidden;
    }

    /// <summary>
    /// Запускает асинхронную инициализацию окна.
    /// </summary>
    public async Task InitializeAsync()
    {
      SystemEventsBinder systemEventsBinder = new SystemEventsBinder();
      UiEventsBinder uiEventsBinder = new UiEventsBinder(this);
      StateEventsBinder stateEventsBinder = new StateEventsBinder(this, usbServices, _consoleManager);

      ApplicationEvents = new ApplicationEventsBinder(systemEventsBinder, uiEventsBinder, stateEventsBinder);
      ApplicationEvents.BindAll();

      await Task.Run(async () =>
      {
        try
        {
          await StartConfigAsync();
        }
        catch (InvalidOperationException exception)
        {
          LogError($"Ошибка загрузки темы программы: {exception}");
          return;
        }
        catch (Exception ex)
        {
          string errorDetails = GetErrorDetails(ex);
          LogError($"Ошибка выполнения программы: {errorDetails}");
          MessageBox.Show($"Ошибка: {errorDetails}");
        }
      });

      SettingsGUI();
      ProcessCommandLineArgs();
      _searchWindow = new SearchWindow();
    }

    /// <summary>
    /// Обрабатывает аргументы командной строки.
    /// Если аргументы не содержат "admin", USB мониторинг отключается; иначе включается.
    /// </summary>
    private void ProcessCommandLineArgs()
    {
      string[] args = App.CommandLineArgs;

      if (!args.Contains("admin"))
      {
        usbServices.SetUsbMonitoring(false);
      }
      else
      {
        usbServices.SetUsbMonitoring(true);
      }
    }

    /// <summary>
    /// Настраивает визуальное оформление окна и инициализирует информационный блок.
    /// Скрывает панель администратора и добавляет обработку команды активации пункта меню.
    /// </summary>
    private void SettingsGUI()
    {
      _infoBlock = InfoBlock;
      this.Admin.Visibility = Visibility.Collapsed;
      this.CommandBindings.Add(new CommandBinding(ActivateMenuItemCommand, ExecuteActivateMenuItem));
      LogInformation("Главное окно инициализировано.");
    }

    /// <summary>
    /// Возвращает подробное описание ошибки для отображения пользователю.
    /// </summary>
    /// <param name="ex">Исключение, вызвавшее ошибку.</param>
    /// <returns>Строка с описанием ошибки.</returns>
    private string GetErrorDetails(Exception ex)
    {
      return $"{ex.Message}";
    }

    /// <summary>
    /// Асинхронно загружает все настройки.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию чтения настроек.</returns>
    private async Task StartConfigAsync()
    {
      await Initialize();
    }
  }
}
