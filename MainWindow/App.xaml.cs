using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Diagnostics.Abstractions;
using Ask.Support;
using ConsoleUI.ConsoleLogic;
using MainWindowProgram.Init;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
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
      var args = Environment.GetCommandLineArgs().Skip(1).ToArray();

      if (!SingleInstanceManager.CheckOrSignal(args))
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
      RegisterGlobalExceptionHandlers();
      CommandLineArgs = e.Args;
      FileAssociationRegistrar.RegisterCurrentUserAssociations();
      ApplicationClockService.Start();

      SplashScreenManager.ShowSplash();
      Console.SetOut(new ConsoleRedirector());

      await Task.Run(async () =>
      {
        await PreStartupInitializer.Initialize();
        await InitializeTheme();
      });

      base.OnStartup(e);

      try
      {
        var mainWindow = new MainWindow
        {
          Visibility = Visibility.Hidden
        };
        Application.Current.MainWindow = mainWindow;

        await mainWindow.InitializeAsync();
        ApplicationActivator.FlushPendingFileRequests();

        await SplashScreenManager.CloseSplashAsync();

        SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
        mainWindow.Visibility = Visibility.Visible;

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

        LogError("FATAL OnStartup exception");
        LogError(ex.ToString());
        CreateCrashPackage(ex, "OnStartup");
        Environment.FailFast("Fatal startup error", ex);

        Message.MessageBoxCustom.Show("Произошла ошибка запуска приложения. Сообщите о данной ошибке вашему администратору или повторите попытку.", "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
        Application.Current.Shutdown();
      }
    }

    private void RegisterGlobalExceptionHandlers()
    {
      LogInformation("GLOBAL: Регистрация обработчиков исключений");

      DispatcherUnhandledException += (s, e) =>
      {
        LogError("GLOBAL UI EXCEPTION:");
        LogError(e.Exception.ToString());

        CreateCrashPackage(e.Exception, "DispatcherUnhandledException");
      };

      AppDomain.CurrentDomain.UnhandledException += (s, e) =>
      {
        LogError("GLOBAL DOMAIN EXCEPTION:");

        if (e.ExceptionObject is Exception ex)
        {
          LogError(ex.ToString());
          CreateCrashPackage(ex, "AppDomain.UnhandledException");
        }
        else
        {
          LogError("Не-Exception объект");
        }
      };

      TaskScheduler.UnobservedTaskException += (s, e) =>
      {
        LogError("GLOBAL TASK EXCEPTION:");
        LogError(e.Exception.ToString());

        CreateCrashPackage(e.Exception, "TaskScheduler.UnobservedTaskException");

        e.SetObserved();
      };
    }

    private static void CreateCrashPackage(Exception ex, string source)
    {
      try
      {
        ex.Data["CrashSource"] = source;
        var service = PreStartupInitializer.AppHost?.Services.GetService<ICrashPackageService>();
        if (service == null)
        {
          SaveFatalInfo(ex, source);
          return;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        _ = service.CreateAsync(ex, cts.Token).GetAwaiter().GetResult();
      }
      catch (Exception packageException)
      {
        LogException(packageException, customMessage: $"Crash package creation failed for {source}");
        SaveFatalInfo(ex, source);
      }
    }

    private static void SaveFatalInfo(Exception ex, string source)
    {
      try
      {
        var dir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "CrashReports");

        Directory.CreateDirectory(dir);

        var file = Path.Combine(
            dir,
            $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log");

        File.WriteAllText(file,
          $"""
          Источник: {source}
          Время: {DateTime.Now}
          Версия: {Assembly.GetEntryAssembly()?.GetName().Version}
          
          OS: {Environment.OSVersion}
          64-bit: {Environment.Is64BitProcess}
          CLR: {Environment.Version}
          
          ИСКЛЮЧЕНИЕ:
          {ex}
          """);
      }
      catch
      {

      }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
      base.OnExit(e);
      ApplicationClockService.Stop();

      try
      {
        var svc = BreakdownTesters.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault();
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
