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

      SplashWindow loadWindow = new SplashWindow();
      loadWindow.Show();

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
  }
}
