using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ConsoleUtilities;
using Utilities.USB;
using static AppConfig.EventAggregator;
using static AppConfig.SettingsFileReader;
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
    MessageHandler messageHandler = new MessageHandler(infoBlock: _infoBlock);

    /// <summary>
    /// Сервис мониторинга USB, работающий с диспетчером приложения.
    /// </summary>
    static private USBMonitorService usbMonitorService = new USBMonitorService(Application.Current.Dispatcher);

    // Менеджер консоли (Singleton), отвечающий за переключение режима консоли и обработку событий администратора.
    private readonly ConsoleManager _consoleManager;

    /// <summary>
    /// Инициализирует новое окно приложения и настраивает события, командную строку, мониторинг USB и конфигурацию.
    /// </summary>
    public MainWindow()
    {
      InitializeComponent();
      _consoleManager = ConsoleManager.Instance;
      this.Visibility = Visibility.Hidden;
    }

    /// <summary>
    /// Запускает асинхронную инициализацию окна.
    /// </summary>
    /// <returns></returns>
    public async Task InitializeAsync()
    {
      _consoleManager.AdminModeChanged += _consoleManager_AdminModeChanged;
      SetEvent();

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
      this.PreviewKeyDown += OnKeyDown;
    }

    /// <summary>
    /// Обработчик события изменения режима администратора от менеджера консоли.
    /// Останавливает или запускает мониторинг USB в зависимости от новых прав.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Новое значение режима администратора.</param>
    private void _consoleManager_AdminModeChanged(object? sender, bool e)
    {
      if (e)
      {
        StopUsbMonitoring();
        OnAdminRightsChangedHandler(null, true);
      }
      else
      {
        OnAdminRightsChangedHandler(null, false);
        SetUsbMonitoring(false);
      }
    }

    /// <summary>
    /// Обрабатывает нажатия клавиш в главном окне.
    /// Если нажаты Ctrl + Oem3, переключает видимость консоли.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события нажатия клавиши.</param>
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
      if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.Oem3)
      {
        _consoleManager.ToggleConsole();
        e.Handled = true;
      }
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
        SetUsbMonitoring(false);
      }
      else
      {
        LogInformation("Запущен в режиме администратора через аргумент командной строки.");
        SetUsbMonitoring(true);
      }
    }

    /// <summary>
    /// Устанавливает обработчики событий для окна и приложения.
    /// Подписывается на события закрытия окна, изменения размера, необработанных исключений и изменений состояний.
    /// </summary>
    private void SetEvent()
    {
      this.Closing += MainWindow_Closing;
      this.PreviewKeyDown += MainWindow_PreviewKeyDown;
      this.SizeChanged += MainWindow_SizeChanged;

      AppDomain.CurrentDomain.UnhandledException += App.CurrentDomain_UnhandledException;
      Application.Current.DispatcherUnhandledException += App.DispatcherUnhandledException;
      LockedChanged += ApplicationDataHandler_LockedChanged;
      AdminRightsChanged += ApplicationDataHandler_AdminRightsChanged;
      usbMonitorService.AdminRightsChanged += OnAdminRightsChangedHandler;
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
      await ReadAllSettingsAsync();
    }

    /// <summary>
    /// Включает или отключает мониторинг USB в зависимости от режима администратора.
    /// </summary>
    /// <param name="admin">Если <c>true</c> — включить режим администратора, иначе — отключить.</param>
    private void SetUsbMonitoring(bool admin)
    {
      if (!admin)
      {
        usbMonitorService.Start();
      }
      else
      {
        usbMonitorService.AdminRights = admin;
      }
    }

    /// <summary>
    /// Останавливает сервис мониторинга USB.
    /// </summary>
    private void StopUsbMonitoring()
    {
      usbMonitorService.Stop();
    }

    /// <summary>
    /// Обработчик события изменения прав администратора.
    /// Обновляет состояние прав администратора в системном менеджере.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="newRights">Новое состояние прав администратора.</param>
    private void OnAdminRightsChangedHandler(object sender, bool newRights)
    {
      AppConfig.Config.SystemStateManager.SetAdminRights(newRights).ConfigureAwait(true);
    }
  }
}
