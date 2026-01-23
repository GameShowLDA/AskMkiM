using Photino.NET;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Support
{
  public static class HelpProvider
  {
    //private static HelpViewerWindow _helpWindow;

    private static PhotinoWindow _helpWindow;

    private static bool _settingsSet = false;

    // 1) Для совместимости: Func<string>-провайдер старого типа
    public static readonly DependencyProperty HelpKeyProviderProperty =
        DependencyProperty.RegisterAttached(
            "HelpKeyProvider",
            typeof(Func<string>),
            typeof(HelpProvider),
            new PropertyMetadata(null));

    public static void SetHelpKeyProvider(DependencyObject element, Func<string> provider)
        => element.SetValue(HelpKeyProviderProperty, provider);

    public static Func<string>? GetHelpKeyProvider(DependencyObject element)
        => element.GetValue(HelpKeyProviderProperty) as Func<string>;

    // 2) Новое простое string-свойство для ключа справки
    public static readonly DependencyProperty HelpKeyProperty =
        DependencyProperty.RegisterAttached(
            "HelpKey",
            typeof(string),
            typeof(HelpProvider),
            new PropertyMetadata(null));

    public static void SetHelpKey(DependencyObject element, string key)
        => element.SetValue(HelpKeyProperty, key);

    public static string? GetHelpKey(DependencyObject element)
        => (string?)element.GetValue(HelpKeyProperty);

    // 3) Храним последний элемент под мышью
    private static DependencyObject? _lastHoverElement;

    /// <summary>
    /// Вызывается один раз в конструкторе окна: устанавливает обработчики MouseMove и F1.
    /// </summary>
    public static void RegisterHelp(Window window)
    {
      window.PreviewMouseMove += (s, e) =>
      {
        _lastHoverElement = e.OriginalSource as DependencyObject;
      };

      window.PreviewKeyDown += (s, e) =>
      {
        if (e.Key != Key.F1) return;

        DependencyObject? el = _lastHoverElement
                              ?? Keyboard.FocusedElement as DependencyObject
                              ?? Mouse.DirectlyOver as DependencyObject;

        string command = "";

        while (el != null)
        {
          var simpleKey = GetHelpKey(el);
          if (!string.IsNullOrWhiteSpace(simpleKey))
          {
            command = simpleKey!;
            break;
          }

          var provider = GetHelpKeyProvider(el);
          if (provider != null)
          {
            var txt = provider().Trim();
            if (!string.IsNullOrWhiteSpace(txt))
            {
              command = txt;
              break;
            }
          }

          el = VisualTreeHelper.GetParent(el);
        }

        if (string.IsNullOrWhiteSpace(command))
        {
          var tagEl = FindElementWithTag(_lastHoverElement);
          if (tagEl is FrameworkElement fe && fe.Tag is string tag)
            command = tag.Trim();
        }

        ShowHelp(command);
        e.Handled = true;
      };
    }

    private static FrameworkElement? FindElementWithTag(DependencyObject? element)
    {
      while (element != null)
      {
        if (element is FrameworkElement fe && fe.Tag is string)
          return fe;
        element = VisualTreeHelper.GetParent(element);
      }
      return null;
    }

    /// <summary>
    /// Формирует URL и открывает окно справки или браузер.
    /// </summary>
    public static void ShowHelp(string command)
    {
      if (_helpWindow == null)
      {
        _helpWindow = new();
        _settingsSet = false;
      }

        var helpDir = Path.Combine(
          AppDomain.CurrentDomain.BaseDirectory,
          "AppHelp");
      if (!Directory.Exists(helpDir))
      {
        MessageBox.Show("Папка с справкой не найдена.", "Справка");
        return;
      }

      //try
      //{
      //  HelpServer.EnsureStarted();
      //}
      //catch (Exception ex)
      //{
      //  MessageBox.Show("Не удалось открыть справку!\nОбратитесь к разработчику!", "Справка");
      //  LogError($"Не удалось запустить Help-сервер: {ex.Message}");
      //  return;
      //}

      string url = string.IsNullOrWhiteSpace(command)
          ? "/index.html"
          : $"/index.html?cmd={Uri.EscapeDataString(command)}";

      OpenHelpViewer(url);

      //var e = new PhotinoWindow()
      //      .SetTitle("My First Photino.NET Application")
      //      .SetUseOsDefaultLocation(true)
      //      .SetSize(800, 600)
      //      .Load($"http://localhost:{HelpServer.Port}" + url)
      //      .RegisterWebMessageReceivedHandler((sender, message) =>
      //      {
      //        Console.WriteLine($"Message received from frontend: {message}");
      //      })
      //      .Center();
      //e.WaitForClose();
    }

    public static void OpenFastMenuCommand() =>
      OpenHelpViewer("/FastMenuCommand.html");

    private static void OpenHelpViewer(string relativeFileAddress)
    {
      if (_settingsSet == false)
      {
        SetSettings(_helpWindow);
        _settingsSet = true;
      }
      _helpWindow.Load($"http://localhost:{HelpServer.Port}" + relativeFileAddress);

      try
      {
        _helpWindow.WaitForClose();
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Ошибка открытия Help Viewer: {ex}");
      }

      //try
      //{
      //  if (Application.Current.Dispatcher.CheckAccess())
      //    ShowHelpWindow($"http://localhost:{HelpServer.Port}" + relativeFileAddress);
      //  else
      //    Application.Current.Dispatcher.Invoke(() => ShowHelpWindow($"http://localhost:{HelpServer.Port}" + relativeFileAddress));
      //}
      //catch (Exception ex)
      //{
      //  Debug.WriteLine($"Ошибка открытия просмотрщика справочника: {ex}");
      //  Process.Start(new ProcessStartInfo($"http://localhost:{HelpServer.Port}" + relativeFileAddress) { UseShellExecute = true });
      //}
    }

    private static void SetSettings(PhotinoWindow window)
    {
      window
        .SetTitle("Справочная система")
        .SetUseOsDefaultLocation(true)
        .SetSize(1024, 768)
        .SetMinSize(600, 600)
        .RegisterWebMessageReceivedHandler((sender, message) =>
        {
          LogDebug(
            $"A JavaScript message from the HELP-system:\n" +
            $"Object: {sender}\n" +
            $"Message: {message}"
            );
        }
        )
        .Center();
    }

    //private static void ShowHelpWindow(string url)
    //{
    //  if (_helpWindow == null || !_helpWindow.IsLoaded)
    //  {
    //    _helpWindow = new HelpViewerWindow();
    //    _helpWindow.Closed += (s, e) => _helpWindow = null;
    //  }

    //  _helpWindow.Navigate(url);
    //  _helpWindow.Show();
    //  _helpWindow.Activate();
    //  _helpWindow.Focus();
    //}
  }
}