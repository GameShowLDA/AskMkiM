using System;
using System.Diagnostics;
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

      // Отметить исключение как обработанное, чтобы не падало приложение
      e.SetObserved();

      // Логирование основной ошибки
      LogException("Необработанное исключение в TaskScheduler", ex);

      // Отладочный вывод полного стека вызова
      LogDebug("=== UnobservedTaskException ===");
      LogDebug(ex.ToString());

      // Обработка AggregateException
      if (ex is AggregateException aggEx)
      {
        foreach (var inner in aggEx.InnerExceptions)
        {
          LogException("Вложенное исключение в TaskScheduler", inner);
          LogDebug(inner.ToString());
        }
      }
      else if (ex.InnerException is not null)
      {
        LogException("Вложенное исключение в TaskScheduler", ex.InnerException);
        LogDebug(ex.InnerException.ToString());
      }
    }

    static private void MessageProtocol(Exception ex)
    {
      // AppConfiguration.Services.UserMessageServiceProvider.ShowMessageAsync(new ShowMessageModel("FATAL ERROR", ShowMessageModel.ErrorMessage.TitleColor, ex.Message)).ConfigureAwait(true);
    }
  }
}
