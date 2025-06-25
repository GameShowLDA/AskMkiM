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
    // Attached property для Func<EnumHelpCommands>
    public static readonly DependencyProperty HelpKeyProviderProperty =
        DependencyProperty.RegisterAttached(
            "HelpKeyProvider",
            typeof(Func<EnumHelpCommands>),
            typeof(HelpProvider),
            new PropertyMetadata(null));

    public static void SetHelpKeyProvider(DependencyObject element, Func<EnumHelpCommands> provider)
        => element.SetValue(HelpKeyProviderProperty, provider);

    public static Func<EnumHelpCommands>? GetHelpKeyProvider(DependencyObject element)
        => element.GetValue(HelpKeyProviderProperty) as Func<EnumHelpCommands>;

    public enum EnumHelpCommands
    {
      [Description("КЦ")] KTs,
      [Description("ОК")] OK,
      [Description("ПИ")] PI,
      [Description("ПР")] PR,
      [Description("РМ")] RM,
      [Description("СИ")] SI,
      [Description("СП")] SP,
      [Description("ЦУ")] TsU,
      [Description("УП")] UP
    }

    /// <summary>
    /// Регистрирует глобальный обработчик F1.
    /// </summary>
    public static void RegisterHelp(Window window)
    {
      window.PreviewKeyDown += (sender, e) =>
      {
        if (e.Key != Key.F1) return;

        // Пытаемся получить команду из attached provider
        DependencyObject? current = Keyboard.FocusedElement as DependencyObject
                                  ?? Mouse.DirectlyOver as DependencyObject;
        while (current != null)
        {
          var provider = GetHelpKeyProvider(current);
          if (provider != null)
          {
            var cmd = provider();
            ShowHelp(cmd);
            e.Handled = true;
            return;
          }
          current = VisualTreeHelper.GetParent(current);
        }

        // fallback: по Tag или по Description
        string? tag = null;
        var focused = Keyboard.FocusedElement as DependencyObject
                      ?? Mouse.DirectlyOver as DependencyObject;
        var fe = FindElementWithTag(focused);
        if (fe != null) tag = fe.Tag as string;

        if (!string.IsNullOrEmpty(tag)
            && TryGetByDescription(tag, out EnumHelpCommands enumFromTag))
        {
          ShowHelp(enumFromTag);
        }
        else
        {
          MessageBox.Show("Элемент справки не найден.", "Справка");
        }
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
    /// Открывает нужный раздел справки по EnumHelpCommands.
    /// </summary>
    public static void ShowHelp(EnumHelpCommands command)
    {
      // Убеждаемся, что папка есть
      var helpDir = Path.Combine(
          AppDomain.CurrentDomain.BaseDirectory,
          "Help", "AppHelp");
      if (!Directory.Exists(helpDir))
      {
        MessageBox.Show("Папка с справкой не найдена.", "Справка");
        return;
      }

      // Запускаем HTTP-сервер (если ещё не запущен)
      try
      {
        HelpServer.EnsureStarted();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Не удалось запустить Help-сервер: {ex.Message}", "Справка");
        return;
      }

      // Формируем URL вида http://localhost:8000/index.html?cmd=TsU
      string url = $"http://localhost:{HelpServer.Port}/index.html?cmd={command}";

      // Открываем в браузере
      Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    /// <summary>
    /// Парсит enum по его [Description("...")] атрибуту.
    /// </summary>
    public static bool TryGetByDescription(string description, out EnumHelpCommands result)
    {
      foreach (var field in typeof(EnumHelpCommands).GetFields(BindingFlags.Public | BindingFlags.Static))
      {
        var attr = field.GetCustomAttribute<DescriptionAttribute>();
        if (attr != null && attr.Description.Equals(description, StringComparison.OrdinalIgnoreCase))
        {
          result = (EnumHelpCommands)field.GetValue(null)!;
          return true;
        }
      }
      result = default;
      return false;
    }
  }
}