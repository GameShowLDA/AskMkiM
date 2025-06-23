using System.Runtime.InteropServices;
using System.Windows;
using ConsoleUI.ConsoleCommanding.Services;
using ConsoleUI.ConsoleLogic;
using static Utilities.LoggerUtility;
using System.Runtime.InteropServices;

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
    private const int SW_HIDE = 0;

    [Flags]
    public enum EXECUTION_STATE : uint
    {
      ES_CONTINUOUS = 0x80000000,
      ES_DISPLAY_REQUIRED = 0x00000002,
      ES_SYSTEM_REQUIRED = 0x00000001,
      // ES_AWAYMODE_REQUIRED = 0x00000040 // если хочешь поддержать режим "away"
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
        // 1. Создаём MainWindow (не показываем)
        var mainWindow = new MainWindow
        {
            Visibility = Visibility.Hidden
        };

        // 2. Асинхронно инициализируем MainWindow
        await mainWindow.InitializeAsync();

        // 3. Ждём завершения анимации SplashWindow
        await splashWindow.WaitForCloseAsync();

        // 4. Показываем MainWindow
        SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
        mainWindow.Visibility = Visibility.Visible;
        mainWindow.Closed += (s, _) =>
        {
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        };
      }
      catch (Exception ex)
      {
        LogException(ex, "Произошла ошибка запуска приложения.");
        MessageBox.Show("Произошла ошибка запуска приложения. Сообщите о данной ошибке вашему администратору или повторите попытку.", "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
        Application.Current.Shutdown();
      }
    }
  }
}
