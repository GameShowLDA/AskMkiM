using System.Windows;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Support;

/// <summary>
/// Управляет единственным WPF-окном справочной системы.
/// </summary>
public static class HelpViewerWindow
{
  private static HelpBrowserWindow? _helpWindow;

  /// <summary>
  /// Признак закрытого окна справки.
  /// </summary>
  public static bool _IsClose => _helpWindow == null;

  /// <summary>
  /// Открывает страницу в существующем окне справки и активирует окно.
  /// </summary>
  /// <param name="page">Относительный адрес страницы справки.</param>
  public static void Load(string page) => OnDispatcher(() =>
  {
    var baseUrl = HelpServer.BaseUrl;
    if (baseUrl == null || _helpWindow == null) return;
    _helpWindow.Navigate(new Uri(baseUrl, page));
    Show();
  });

  /// <summary>
  /// Показывает и активирует существующее окно справки без блокировки приложения.
  /// </summary>
  public static void Show() => OnDispatcher(() =>
  {
    if (_helpWindow == null) return;
    if (_helpWindow.WindowState == WindowState.Minimized) _helpWindow.WindowState = WindowState.Normal;
    _helpWindow.Show();
    _helpWindow.Activate();
  });

  /// <summary>
  /// Закрывает окно справки и освобождает браузер.
  /// </summary>
  public static void Close() => OnDispatcher(() => _helpWindow?.Close());

  /// <summary>
  /// Запускает локальный сервер и открывает страницу в единственном окне справки.
  /// </summary>
  /// <param name="page">Относительный адрес страницы справки.</param>
  public static void LoadAndShow(string page) => OnDispatcher(() =>
  {
    try
    {
      HelpServer.EnsureStarted();
      if (HelpServer.BaseUrl == null) return;
      if (_helpWindow == null)
      {
        var window = new HelpBrowserWindow();
        _helpWindow = window;
        window.Closed += (_, _) =>
        {
          if (ReferenceEquals(_helpWindow, window)) _helpWindow = null;
        };
      }
      Load(page);
    }
    catch (Exception ex)
    {
      LogException(ex, customMessage: "Не удалось открыть окно справки.");
    }
  });

  private static void OnDispatcher(Action action)
  {
    var dispatcher = Application.Current?.Dispatcher;
    if (dispatcher == null || dispatcher.HasShutdownStarted) return;
    if (dispatcher.CheckAccess()) action();
    else dispatcher.Invoke(action);
  }
}
