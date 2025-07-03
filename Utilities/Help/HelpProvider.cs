using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static Utilities.LoggerUtility;

namespace Utilities.Help
{
  public static class HelpProvider
  {
    private static HelpViewerWindow _helpWindow;

    // Attached property для Func<EnumHelpCommands>
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

    //public enum EnumHelpCommands
    //{
    //  None,       // Для открытия начальной страницы
    //  Unknown,    // Для нераспознанных команд
    //  [Description("КЦ")] KTs,
    //  [Description("ОК")] OK,
    //  [Description("ПИ")] PI,
    //  [Description("ПР")] PR,
    //  [Description("РМ")] RM,
    //  [Description("СИ")] SI,
    //  [Description("СП")] SP,
    //  [Description("ЦУ")] TsU,
    //  [Description("УП")] UP,
    //}

    /// <summary>
    /// Регистрирует глобальный обработчик F1.
    /// </summary>
    public static void RegisterHelp(Window window)
    {
      window.PreviewKeyDown += (s, e) =>
      {
        if (e.Key != Key.F1) return;

        // 1) получаем строку из провайдера
        DependencyObject? el = Keyboard.FocusedElement as DependencyObject
                              ?? Mouse.DirectlyOver as DependencyObject;
        string command = "";
        while (el != null)
        {
          var prov = GetHelpKeyProvider(el);
          if (prov != null)
          {
            var text = prov().Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
              command = text;
              break;
            }
          }
          el = VisualTreeHelper.GetParent(el);
        }

        // 2) если ничего не нашли, смотрим Tag как fallback
        if (string.IsNullOrWhiteSpace(command))
        {
          var fe = FindElementWithTag(el);
          var tag = fe?.Tag as string;
          command = string.IsNullOrWhiteSpace(tag) ? "" : tag.Trim();
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
    /// Открывает нужный раздел справки.
    /// </summary>
    public static void ShowHelp(string command)
    {
      var helpDir = Path.Combine(
          AppDomain.CurrentDomain.BaseDirectory,
          "Help", "AppHelp");
      if (!Directory.Exists(helpDir))
      {
        MessageBox.Show("Папка с справкой не найдена.", "Справка");
        return;
      }

      try
      {
        HelpServer.EnsureStarted();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Не удалось открыть справку!\nОбратитесь к разработчику!", "Справка");
        LogError($"Не удалось запустить Help-сервер: {ex.Message}");
        return;
      }

      string url;
      if (string.IsNullOrWhiteSpace(command))
      {
        url = $"http://localhost:{HelpServer.Port}/index.html";
      }
      else
      {
        // Экранируем спецсимволы в параметре
        var esc = Uri.EscapeDataString(command);
        url = $"http://localhost:{HelpServer.Port}/index.html?cmd={esc}";
      }

      OpenHelpViewer(url);
    }


    //private static string GetCommandDescription(EnumHelpCommands command)
    //{
    //  var field = typeof(EnumHelpCommands).GetField(command.ToString());
    //  if (field == null) return command.ToString();

    //  var attribute = field.GetCustomAttribute<DescriptionAttribute>();
    //  return attribute?.Description ?? command.ToString();
    //}

    //private static void OpenInitialHelp()
    //{
    //  // Убеждаемся, что папка есть
    //  var helpDir = Path.Combine(
    //      AppDomain.CurrentDomain.BaseDirectory,
    //      "Help", "AppHelp");
    //  if (!Directory.Exists(helpDir))
    //  {
    //    MessageBox.Show("Папка с справкой не найдена.", "Справка");
    //    return;
    //  }

    //  // Запускаем HTTP-сервер
    //  try
    //  {
    //    HelpServer.EnsureStarted();
    //  }
    //  catch (Exception ex)
    //  {
    //    MessageBox.Show($"Не удалось запустить Help-сервер: {ex.Message}", "Справка");
    //    return;
    //  }

    //  string url = $"http://localhost:{HelpServer.Port}/index.html";
    //  OpenHelpViewer(url);
    //}

    private static void OpenHelpViewer(string url)
    {
      try
      {
        if (Application.Current.Dispatcher.CheckAccess())
        {
          ShowHelpWindow(url);
        }
        else
        {
          Application.Current.Dispatcher.Invoke(() => ShowHelpWindow(url));
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Ошибка открытия Help Viewer: {ex}");
        // Fallback: открытие в системном браузере
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
      }
    }

    private static void ShowHelpWindow(string url)
    {
      if (_helpWindow == null || !_helpWindow.IsLoaded)
      {
        _helpWindow = new HelpViewerWindow();
        _helpWindow.Closed += (s, e) => _helpWindow = null;
      }

      _helpWindow.Navigate(url);
      _helpWindow.Show();
      _helpWindow.Activate();
      _helpWindow.Focus();
    }

    /// <summary>
    /// Парсит enum по его [Description("...")] атрибуту.
    /// </summary>
    //public static bool TryGetByDescription(string description, out EnumHelpCommands result)
    //{
    //  foreach (var field in typeof(EnumHelpCommands).GetFields(BindingFlags.Public | BindingFlags.Static))
    //  {
    //    var attr = field.GetCustomAttribute<DescriptionAttribute>();
    //    if (attr != null && attr.Description.Equals(description, StringComparison.OrdinalIgnoreCase))
    //    {
    //      result = (EnumHelpCommands)field.GetValue(null)!;
    //      return true;
    //    }
    //  }
    //  result = default;
    //  return false;
    //}
  }
}