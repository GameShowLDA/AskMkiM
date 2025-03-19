using System.Windows;
using static Utilities.LoggerUtility;

namespace MainWindowProgram
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    static internal void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      Exception ex = (Exception)e.ExceptionObject;
      LogError("Необработанное исключение в AppDomain: " + ex.Message);
    }

    // Обработчик необработанных исключений в главном потоке (UI)
    static internal new void DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
      Exception ex = e.Exception;
      LogError("Необработанное исключение в Dispatcher: " + ex.Message);
      e.Handled = true;
    }

    public static string[] CommandLineArgs { get; private set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);
      CommandLineArgs = e.Args; // Сохраняем аргументы

      SplashWindow loadWindow = new SplashWindow();
      loadWindow.Show();

      MainWindow mainWindow = null;

      // 2. Создаем MainWindow в UI-потоке
      await Dispatcher.InvokeAsync(() =>
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


  }
}
