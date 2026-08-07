using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Ask.Engine.UnitTests.TestInfrastructure;

/// <summary>
/// Выполняет WPF-зависимые тесты в едином STA-потоке.
/// </summary>
internal static class WpfTestHost
{
  private static readonly Lazy<Dispatcher> DispatcherInstance = new(CreateDispatcher);

  /// <summary>
  /// Выполняет асинхронное действие в STA-потоке WPF.
  /// </summary>
  /// <param name="action">Асинхронное действие теста.</param>
  public static Task RunAsync(Func<Task> action)
  {
    var dispatcher = DispatcherInstance.Value;
    return dispatcher.InvokeAsync(action).Task.Unwrap();
  }

  /// <summary>
  /// Выполняет асинхронную функцию в STA-потоке WPF.
  /// </summary>
  /// <typeparam name="T">Тип результата функции.</typeparam>
  /// <param name="action">Асинхронная функция теста.</param>
  /// <returns>Результат функции.</returns>
  public static Task<T> RunAsync<T>(Func<Task<T>> action)
  {
    var dispatcher = DispatcherInstance.Value;
    return dispatcher.InvokeAsync(action).Task.Unwrap();
  }

  private static Dispatcher CreateDispatcher()
  {
    var ready = new TaskCompletionSource<Dispatcher>(TaskCreationOptions.RunContinuationsAsynchronously);

    var thread = new Thread(() =>
    {
      try
      {
        EnsureApplicationWithResources();
        ready.SetResult(Dispatcher.CurrentDispatcher);
        Dispatcher.Run();
      }
      catch (Exception exception)
      {
        ready.TrySetException(exception);
      }
    })
    {
      IsBackground = true,
    };

    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();

    return ready.Task.GetAwaiter().GetResult();
  }

  private static void EnsureApplicationWithResources()
  {
    var application = Application.Current ?? new Application();
    application.Resources["TestsProtocolMessageSuccesForeground"] = new SolidColorBrush(Colors.Green);
    application.Resources["TestsProtocolMessageErrorForeground"] = new SolidColorBrush(Colors.Red);
    application.Resources["TestsProtocolHeaderForeground"] = new SolidColorBrush(Colors.White);
    application.Resources["TestsProtocolMessageForeground"] = new SolidColorBrush(Colors.White);
    application.Resources["TestsProtocolTimeForeground"] = new SolidColorBrush(Colors.White);
    application.Resources["YellowColorSolidColorBrush"] = new SolidColorBrush(Colors.Yellow);
    application.Resources["LightBlueColorSolidColorBrush"] = new SolidColorBrush(Colors.LightBlue);
  }
}
