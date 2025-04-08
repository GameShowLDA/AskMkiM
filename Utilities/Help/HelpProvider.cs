using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Utilities.Help
{
  /// <summary>
  /// Обрабатывает нажатие F1 и отображает информацию о текущем элементе.
  /// </summary>
  public static class HelpProvider
  {
    /// <summary>
    /// Регистрирует глобальный обработчик клавиши F1.
    /// </summary>
    public static void RegisterHelp(Window window)
    {
      window.PreviewKeyDown += (sender, e) =>
      {
        if (e.Key == Key.F1)
        {
          var element = Mouse.DirectlyOver as DependencyObject ?? Keyboard.FocusedElement as DependencyObject;
          var target = FindElementWithTag(element);

          if (target is FrameworkElement fe && fe.Tag is string helpKey)
          {
            ShowHelp(helpKey);
          }
          else
          {
            MessageBox.Show("Элемент справки не найден.", "Справка");
          }

          e.Handled = true;
        }
      };
    }

    /// <summary>
    /// Открывает HTML-страницу справки, соответствующую ключу, выполняя поиск по всей папке AppHelp.
    /// </summary>
    public static void ShowHelp(string helpKey)
    {
      string helpFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Help", "AppHelp", "index.html");

      if (File.Exists(helpFile))
      {
        string uri = new Uri(helpFile).AbsoluteUri + $"?section={helpKey}";
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
      }
      else
      {
        MessageBox.Show("Файл справки index.html не найден.", "Справка");
      }
    }

    /// <summary>
    /// Ищет ближайший элемент с заданным Tag.
    /// </summary>
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
  }
}
