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
  }
}
