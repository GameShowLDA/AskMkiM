using System.Runtime.InteropServices;
using System.Windows;
using ConsoleUI.ConsoleCommanding.Services;
using ConsoleUI.ConsoleLogic;
using static Utilities.LoggerUtility;

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

    // Менеджер консоли (Singleton), отвечающий за переключение режима консоли и обработку событий администратора.
    static internal ConsoleManager _consoleManager { get; private set; }

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

      CommandLineArgs = e.Args; // Сохраняем аргументы

      Console.SetOut(new ConsoleRedirector());

      SplashWindow loadWindow = new SplashWindow();
      loadWindow.Show();

      try
      {
        MainWindow mainWindow = null;

        // 2. Создаем MainWindow в UI-потоке
        Dispatcher.Invoke(() =>
        {
          mainWindow = new MainWindow();
          mainWindow.Visibility = Visibility.Hidden; // Делаем его невидимым до закрытия SplashWindow
        });

        // 3. Инициализируем MainWindow (в фоне)
        await mainWindow.InitializeAsync();

        // 4. Ждём завершения анимации SplashWindow
        await loadWindow.WaitForCloseAsync(); // Ждёт плавное исчезновение

        // 5. Только теперь показываем основное окно
        await Dispatcher.InvokeAsync(() =>
        {
          mainWindow.Visibility = Visibility.Visible;
        });
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
