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
    static System.Timers.Timer timer = new System.Timers.Timer();
    static TextBlock _infoBlock;
    private bool isLocked = false;
    MessageHandler messageHandler = new MessageHandler(infoBlock: _infoBlock);
    static private USBMonitorService usbMonitorService = new USBMonitorService(Application.Current.Dispatcher);

    private readonly ConsoleManager _consoleManager;

    public MainWindow()
    {
      InitializeComponent();
      _consoleManager = ConsoleManager.Instance;
      _consoleManager.AdminModeChanged += _consoleManager_AdminModeChanged;
      SetEvent();

      Task.Run(async () =>
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

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
      if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.Oem3) // Ctrl + Ё
      {
        _consoleManager.ToggleConsole();
        e.Handled = true;
      }
    }

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

    private void SettingsGUI()
    {
      _infoBlock = InfoBlock;
      this.Admin.Visibility = Visibility.Collapsed;
      this.CommandBindings.Add(new CommandBinding(ActivateMenuItemCommand, ExecuteActivateMenuItem));
      LogInformation("Главное окно инициализировано.");
    }

    private string GetErrorDetails(Exception ex)
    {
      return $"{ex.Message}";
    }

    private async Task StartConfigAsync()
    {
      await ReadAllSettingsAsync();
    }

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

    private void StopUsbMonitoring()
    {
      usbMonitorService.Stop();
    }

    private void OnAdminRightsChangedHandler(object sender, bool newRights)
    {
      AppConfig.Config.SystemStateManager.SetAdminRights(newRights).ConfigureAwait(true);
    }
  }
}
