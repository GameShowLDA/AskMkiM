using System.Windows;

namespace MainWindowProgram.Init
{
  /// <summary>
  /// Управляет отображением и закрытием окна-заставки (SplashWindow) 
  /// в отдельном UI-потоке.
  /// </summary>
  internal static class SplashScreenManager
  {
    private static Thread? _splashThread;
    private static Window? _splashWindow;

    /// <summary>
    /// Запускает окно-заставку (<see cref="SplashWindow"/>) в отдельном UI-потоке.
    /// </summary>
    /// <remarks>
    /// Создаёт новый STA-поток, в котором создаётся и отображается окно SplashWindow.
    /// Используется <see cref="ManualResetEvent"/>, чтобы дождаться загрузки окна перед продолжением выполнения программы.
    /// </remarks>
    public static void ShowSplash()
    {
      var splashStarted = new ManualResetEvent(false);

      _splashThread = new Thread(() =>
      {
        _splashWindow = new SplashWindow();
        _splashWindow.Loaded += (_, _) => splashStarted.Set();

        _splashWindow.Show();
        System.Windows.Threading.Dispatcher.Run();
      });

      _splashThread.SetApartmentState(ApartmentState.STA);
      _splashThread.IsBackground = true;
      _splashThread.Start();

      splashStarted.WaitOne();
    }

    /// <summary>
    /// Закрывает окно-заставку, если оно активно.
    /// </summary>
    /// <remarks>
    /// Безопасно вызывает закрытие окна в его родном Dispatcher-потоке, 
    /// а затем завершает цикл сообщений и очищает ссылки на объекты.
    /// </remarks>
    public static async Task CloseSplashAsync()
    {
      if (_splashWindow == null || _splashThread == null)
        return;

      var tcs = new TaskCompletionSource<bool>();

      _splashWindow.Dispatcher.Invoke(() =>
      {
        // подписка на завершение анимации
        if (_splashWindow is SplashWindow splash)
        {
          _ = splash.WaitForCloseAsync().ContinueWith(_ =>
          {
            tcs.TrySetResult(true);
          });
        }
        else
        {
          _splashWindow.Close();
          tcs.TrySetResult(true);
        }
      });

      await tcs.Task;

      _splashWindow.Dispatcher.InvokeShutdown();
      _splashWindow = null;
      _splashThread = null;
    }

  }
}
