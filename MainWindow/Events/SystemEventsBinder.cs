using System;
using System.Windows;
using Utilities.Models;
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
      TaskScheduler.UnobservedTaskException += App_TaskScheduler_UnobservedTaskException;
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
      MessageProtocol(ex);
    }

    private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
      Exception ex = e.Exception;
      LogException("Необработанное исключение в Dispatcher", ex);
      e.Handled = true;
      MessageProtocol(ex);
    }

    /// <summary>
    /// Обрабатывает необработанные исключения в забытых Task (например, async void).
    /// </summary>
    private void App_TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
      Exception ex = e.Exception;
      LogException("Необработанное исключение в TaskScheduler", ex, onlyProjectStack: true);
      e.SetObserved();
      MessageProtocol(ex);
    }

    static private void MessageProtocol(Exception ex)
    {
      // AppConfiguration.Services.UserMessageServiceProvider.ShowMessageAsync(new ShowMessageModel("FATAL ERROR", ShowMessageModel.ErrorMessage.TitleColor, ex.Message)).ConfigureAwait(true);
    }
  }
}
