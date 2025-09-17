using AppConfiguration;
using ConsoleUI.ConsoleCommanding.Services;
using ConsoleUI.ConsoleLogic;
using DataBaseConfiguration.Services.Device;
using Microsoft.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewCore.Base.Interface.Main;
using NewCore.Device;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using static Utilities.LoggerUtility;


namespace MainWindowProgram
{
  /// <summary>
  /// Interaction logic for App.xaml.
  /// Класс приложения, отвечающий за запуск и обработку необработанных исключений.
  /// </summary>
  public partial class App : Application
  {
    public static IHost AppHost { get; private set; }
    [DllImport("kernel32.dll")] private static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_HIDE = 0;

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

    /// <summary>
    /// Запускает приложение.
    /// </summary>
    /// <param name="e"></param>
    protected override async void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      CommandLineArgs = e.Args;
      Console.SetOut(new ConsoleRedirector());

      var splashWindow = new SplashWindow();
      splashWindow.Show();

      try
      {
        var mainWindow = new MainWindow
        {
          Visibility = Visibility.Hidden
        };

        await mainWindow.InitializeAsync();

        AppHost = Host.CreateDefaultBuilder()
          .ConfigureServices(svc =>
          {
            svc.AddSingleton<IBreakdownTester, GPT79904>();
            svc.AddSingleton<BreakdownTesterServices>();
          }).Build();

        ServiceLocator.Initialize(AppHost);

        await splashWindow.WaitForCloseAsync();

        SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
        mainWindow.Visibility = Visibility.Visible;
        mainWindow.Closed += (s, _) =>
        {
          SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        };

        Application.Current.MainWindow = mainWindow;
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

      var svc = AppConfiguration.ServiceLocator.GetRequired<IBreakdownTester>();
     
      await svc?.ConnectableManager?.DisconnectAsync();
    }
  }
}
