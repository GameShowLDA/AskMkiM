using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    public MainWindow()
    {
      InitializeComponent();
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
      SetUsbMonitoring();
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
      usbMonitorService.AdminRightsChanged += OnAdminRightsChangedHandler; // Подписываемся на событие
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
      StackTrace trace = new StackTrace(ex, true);
      StackFrame frame = trace.GetFrame(0);
      string fileName = frame?.GetFileName() ?? "Неизвестный файл";
      int lineNumber = frame?.GetFileLineNumber() ?? -1;
      return $"{ex.Message} (Файл: {fileName}, строка: {lineNumber})";
    }

    private async Task StartConfigAsync()
    {
      await ReadAllSettingsAsync();
    }

    private void SetUsbMonitoring()
    {
      usbMonitorService.Start();
    }

    private void OnAdminRightsChangedHandler(object sender, bool newRights)
    {
      AppConfig.Config.SystemStateManager.SetAdminRights(newRights).ConfigureAwait(true);
    }
  }
}
