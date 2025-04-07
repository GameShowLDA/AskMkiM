using System.Windows;
using static Utilities.LoggerUtility;

namespace MainWindowProgram.Events
{
  /// <summary>
  /// Устанавливает обработчики для системных событий приложения.
  /// </summary>
  public class SystemEventsBinder
  {
    /// <summary>
    /// Подписывает обработчики на глобальные системные события.
    /// </summary>
    public void Bind()
    {
      AppDomain.CurrentDomain.UnhandledException += App_CurrentDomain_UnhandledException;
      Application.Current.DispatcherUnhandledException += Application_DispatcherUnhandledException;
    }

    /// <summary>
    /// Обрабатывает необработанные исключения домена приложения.
    /// </summary>
    /// <param name="sender">Источник исключения.</param>
    /// <param name="e">Аргументы события с информацией об исключении.</param>
    static internal void App_CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      Exception ex = (Exception)e.ExceptionObject;
      LogException("Необработанное исключение в AppDomain", ex);
    }

    private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
      Exception ex = e.Exception;
      LogException("Необработанное исключение в Dispatcher", ex);
      e.Handled = true;
    }
  }
}
