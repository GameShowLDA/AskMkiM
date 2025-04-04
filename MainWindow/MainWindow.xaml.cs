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
    private readonly MessageHandler messageHandler = new(_infoBlock);

    /// <summary>
    /// Статический UI-элемент, в который выводятся сообщения от системы.
    /// Используется как главный информационный блок в окне приложения.
    /// </summary>
    private static TextBlock _infoBlock;

    /// <summary>
    /// Флаг, указывающий, заблокировано ли текущее состояние интерфейса.
    /// Используется для предотвращения повторных операций, если процесс уже запущен.
    /// </summary>
    private bool isLocked = false;

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

      _usbServices = new UsbServices();

      var multiWindowService = new MultiWindowService(this.MultiWindow);
      var metrologyService = new MetrologyService(multiWindowService);
      var fileService = new FileService(multiWindowService, () => isLocked);
      var testService = new TestService(multiWindowService);
      var settingsService = new SettingsService(multiWindowService);
      var adminService = new AdminServices(this, multiWindowService);
      var windowService = new WindowService(this, mainMenu, ButtonsPanel, () => isLocked);

      _viewModel = new MainWindowViewModel(metrologyService, fileService, testService, settingsService, adminService, windowService);
      this.DataContext = _viewModel;

      this.Visibility = Visibility.Hidden;
    }

    /// <summary>
    /// Запускает асинхронную инициализацию окна.
    /// </summary>
    public async Task InitializeAsync()
    {
      SystemEventsBinder systemEventsBinder = new SystemEventsBinder();
      UiEventsBinder uiEventsBinder = new UiEventsBinder(this);
      StateEventsBinder stateEventsBinder = new StateEventsBinder(this, _usbServices, App._consoleManager);

      _applicationEvents = new ApplicationEventsBinder(systemEventsBinder, uiEventsBinder, stateEventsBinder);
      _applicationEvents.BindAll();

      await Task.Run(async () =>
      {
        try
        {
          await Initialize();
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

      new CommandLineParser(_usbServices).ProcessCommandLineArgs();

      _searchWindow = new SearchWindow();
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
  }
}
