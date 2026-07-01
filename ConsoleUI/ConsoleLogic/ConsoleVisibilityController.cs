using ConsoleUI.ConsoleUI;
using System.Windows;
using System.Windows.Threading;

namespace ConsoleUI.ConsoleLogic
{
  public static class ConsoleVisibilityController
  {
    private static Thread? _uiThread;
    private static Dispatcher? _disp;
    private static ConsoleOverlay? _window;
    private static bool _isEnabled = true;

    /// <summary>
    /// Показать/скрыть консольное окно.
    /// </summary>
    public static void ToggleConsole()
    {
      if (!_isEnabled)
      {
        return;
      }

      if (_disp is null || _window is null)
      {
        StartConsoleThread();
        return;
      }

      _disp.BeginInvoke(() =>
      {
        if (_window!.IsVisible)
          _window.Hide();
        else
          _window.Show();
      });
    }

    /// <summary>
    /// Включить или отключить доступ к консольному окну.
    /// </summary>
    public static void SetEnabled(bool enabled)
    {
      _isEnabled = enabled;

      if (!enabled)
      {
        SetVisible(false);
      }
    }

    /// <summary>
    /// Явно показать или скрыть консольное окно.
    /// </summary>
    public static void SetVisible(bool visible)
    {
      if (visible && !_isEnabled)
      {
        return;
      }

      if (visible && (_disp is null || _window is null))
      {
        StartConsoleThread();
        return;
      }

      _disp?.BeginInvoke(() =>
      {
        if (_window is null)
        {
          return;
        }

        if (visible)
        {
          _window.Show();
        }
        else
        {
          _window.Hide();
        }
      });
    }

    /// <summary>
    /// Выполнить действие в UI-потоке консоли.
    /// </summary>
    public static void Post(Action action) =>
        _disp?.BeginInvoke(action);

    private static void StartConsoleThread()
    {
      _uiThread = new Thread(() =>
      {
        _window = new ConsoleOverlay();

        CloneAppResourcesInto(_window.Resources);

        _disp = _window.Dispatcher;

        _window.Closed += (_, _) =>
        {
          _window = null;
          _disp = null;
          Dispatcher.ExitAllFrames();
        };

        _window.Show();
        Dispatcher.Run();
      });

      _uiThread.SetApartmentState(ApartmentState.STA);
      _uiThread.IsBackground = true;
      _uiThread.Start();
    }

    private static void CloneAppResourcesInto(ResourceDictionary target)
    {
      foreach (var rd in Application.Current.Resources.MergedDictionaries)
      {
        if (rd.Source is { } uri)
          target.MergedDictionaries.Add(new ResourceDictionary { Source = uri });
      }
    }

    ///<summary>Попросить строку ввода (возвращает Task в вызывающем потоке).</summary>
    public static Task<string> ReadLineAsync()
    {
      var tcs = new TaskCompletionSource<string>();

      Post(async () =>
      {
        if (_window is not null)
          tcs.TrySetResult(await _window.ReadLineAsync());
        else
          tcs.TrySetResult(string.Empty);
      });

      return tcs.Task;
    }

    ///<summary>Перевести консоль в/из режима ввода пароля.</summary>
    public static void SetPasswordMode(bool enabled) =>
        Post(() => _window?.SetPasswordMode(enabled));

    ///<summary>Очистить вывод в ConsoleOverlay (безопасно из любого потока).</summary>
    public static void ClearConsole()
        => Post(() => _window?.ClearConsoleUI());
  }
}
