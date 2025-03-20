using System;
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
    /// Обрабатывает необработанные исключения домена приложения.
    /// </summary>
    /// <param name="sender">Источник исключения.</param>
    /// <param name="e">Аргументы события с информацией об исключении.</param>
    static internal void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      Exception ex = (Exception)e.ExceptionObject;
      LogError("Необработанное исключение в AppDomain: " + ex.Message);
    }

    /// <summary>
    /// Обрабатывает необработанные исключения в главном потоке (UI).
    /// </summary>
    /// <param name="sender">Источник исключения.</param>
    /// <param name="e">Аргументы события с информацией об исключении.</param>
    static internal new void DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
      Exception ex = e.Exception;
      LogError("Необработанное исключение в Dispatcher: " + ex.Message);
      e.Handled = true;
    }

    /// <summary>
    /// Содержит аргументы командной строки, переданные при запуске приложения.
    /// </summary>
    public static string[] CommandLineArgs { get; private set; }

    /// <summary>
    /// Метод, вызываемый при запуске приложения.
    /// Сохраняет аргументы командной строки и вызывает базовую реализацию.
    /// </summary>
    /// <param name="e">Аргументы запуска приложения.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);
      CommandLineArgs = e.Args;
    }
  }
}
