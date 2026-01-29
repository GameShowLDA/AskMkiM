using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Support
{
  public static class HelpProvider
  {

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

      var helpDir = Path.Combine(
          AppDomain.CurrentDomain.BaseDirectory,
          "AppHelp");
      if (!Directory.Exists(helpDir))
      {
        MessageBox.Show("Папка с справкой не найдена.", "Справка");
        return;
      }

      string url = string.IsNullOrWhiteSpace(command)
          ? "/index.html"
          : $"/index.html?cmd={Uri.EscapeDataString(command)}";

      if(HelpViewerWindow._IsClose) HelpViewerWindow.LoadAndShow(url);
      else HelpViewerWindow.Load(url);
    }

    public static void OpenFastMenuCommand() 
    {
      if (HelpViewerWindow._IsClose) HelpViewerWindow.LoadAndShow("/FastMenuCommand.html");
      else HelpViewerWindow.Load("/FastMenuCommand.html");
    }
  }
}