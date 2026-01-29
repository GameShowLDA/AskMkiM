using System.Runtime.InteropServices;
using System.Windows;
using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Support;
using ConsoleUI.ConsoleLogic;
using MainWindowProgram.Init;
using UI.Theme;
using static Ask.LogLib.LoggerUtility;

namespace MainWindowProgram
{
  /// <summary>
  /// Interaction logic for App.xaml.
  /// Класс приложения, отвечающий за запуск и обработку необработанных исключений.
  /// </summary>
  public partial class App : Application
  {
    [DllImport("kernel32.dll")] private static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [Flags]
    public enum EXECUTION_STATE : uint
    {
      ES_CONTINUOUS = 0x80000000,
      ES_DISPLAY_REQUIRED = 0x00000002,
      ES_SYSTEM_REQUIRED = 0x00000001,
    }

    [DllImport("kernel32.dll")]
    public static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

    /// <summary>
    /// Содержит аргументы командной строки, переданные при запуске приложения.
    /// </summary>
    public static string[] CommandLineArgs { get; private set; }
    static App()
    {
      if (!SingleInstanceManager.CheckOrSignal())
      {
        Environment.Exit(0);
      }
    }

    public App() { }

    /// <summary>
    /// Запускает приложение.
    /// </summary>
    /// <param name="e"></param>
    protected override async void OnStartup(StartupEventArgs e)
    {
      // Новая реализация загрузки окна
      SplashScreenManager.ShowSplash();

      await Task.Run(async () =>
      {
        await PreStartupInitializer.Initialize();
        await InitializeTheme();
      });

      base.OnStartup(e);

      CommandLineArgs = e.Args;
      Console.SetOut(new ConsoleRedirector());

      try
      {
        var mainWindow = new MainWindow
        {
          Visibility = Visibility.Hidden
        };

        await mainWindow.InitializeAsync();

        await SplashScreenManager.CloseSplashAsync();


        SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
        mainWindow.Visibility = Visibility.Visible;


        Application.Current.MainWindow = mainWindow;

        mainWindow.Topmost = true;


        await mainWindow.Dispatcher.BeginInvoke(new Action(() =>
        {
          mainWindow.Topmost = false;
        }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);

        // отслеживаем закрытие
        mainWindow.Closed += (s, _) =>
        {
          SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        };

        mainWindow.Activate();
        mainWindow.Focus();
      }
      catch (Exception ex)
      {
        LogException(ex, "Произошла ошибка запуска приложения.");
        Message.MessageBoxCustom.Show("Произошла ошибка запуска приложения. Сообщите о данной ошибке вашему администратору или повторите попытку.", "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
        Application.Current.Shutdown();
      }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
      base.OnExit(e);

      try
      {
        var svc = ServiceLocator.GetRequired<IBreakdownTester>();
        LogInformation($"OnExit: IBreakdownTester instance = {svc.GetHashCode()} | {svc.GetType().FullName}");

        svc?.ConnectableManager?.DisconnectAsync();
      }
      catch { }

      GC.Collect();
      GC.WaitForPendingFinalizers();

      HelpViewerWindow.Close();
      HelpServer.Stop();

      // TODO : Раскомментировать, когда будет готово
      // await Core.Communication.CommunicationManager.ResetAllSystem();
      // await Task.Delay(1000);
      // await Core.ManagerShassy.Function.StopPowerAsync(ConfigCollector.GetManagerShassyIp());
    }

    private async Task InitializeTheme()
    {
      ThemeManager.Initialize();
      await LanguageSettings.InitializeAsync();
      await ThemeSettings.InitializeAsync();
    }
  }
}