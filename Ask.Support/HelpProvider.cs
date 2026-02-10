using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Support
{
  /// <summary>
  /// Провайдер справочной системы.
  /// Позволяет привязать к любому визуальному элементу ключ справки,
  /// а затем по нажатию <c>F1</c> открыть соответствующую страницу справки.
  /// </summary>
  public static class HelpProvider
  {

    /// <summary>
    /// Присоединённое свойство, содержащее делегат, который возвращает ключ справки.
    /// Используется для совместимости и/или динамического вычисления ключа.
    /// </summary>
    /// <remarks>
    /// Тип значения — <see cref="Func{TResult}"/>, возвращающий строку-ключ.
    /// Если делегат возвращает пустую/пробельную строку, ключ считается не заданным.
    /// </remarks>
    public static readonly DependencyProperty HelpKeyProviderProperty =
        DependencyProperty.RegisterAttached(
            "HelpKeyProvider",
            typeof(Func<string>),
            typeof(HelpProvider),
            new PropertyMetadata(null));

    /// <summary>
    /// Устанавливает для элемента делегат-провайдер ключа справки.
    /// </summary>
    /// <param name="element">WPF-элемент, на который навешивается ключ справки.</param>
    /// <param name="provider">Делегат, возвращающий строковый ключ справки.</param>
    public static void SetHelpKeyProvider(DependencyObject element, Func<string> provider)
        => element.SetValue(HelpKeyProviderProperty, provider);

    /// <summary>
    /// Получает делегат-провайдер ключа справки, установленный на элементе.
    /// </summary>
    /// <param name="element">WPF-элемент, с которого читается делегат.</param>
    /// <returns>Делегат <see cref="Func{TResult}"/> или <see langword="null"/>, если не задан.</returns>
    public static Func<string>? GetHelpKeyProvider(DependencyObject element)
        => element.GetValue(HelpKeyProviderProperty) as Func<string>;

    /// <summary>
    /// Присоединённое свойство, содержащее строковый ключ справки.
    /// Предпочтительный простой способ привязать страницу справки к элементу.
    /// </summary>
    /// <remarks>
    /// Если строка пустая/пробельная — ключ считается не заданным.
    /// </remarks>
    public static readonly DependencyProperty HelpKeyProperty =
        DependencyProperty.RegisterAttached(
            "HelpKey",
            typeof(string),
            typeof(HelpProvider),
            new PropertyMetadata(null));

    /// <summary>
    /// Устанавливает для элемента строковый ключ справки.
    /// </summary>
    /// <param name="element">WPF-элемент, на который навешивается ключ справки.</param>
    /// <param name="key">Ключ справки.</param>
    public static void SetHelpKey(DependencyObject element, string key)
        => element.SetValue(HelpKeyProperty, key);

    /// <summary>
    /// Получает строковый ключ справки, установленный на элементе.
    /// </summary>
    /// <param name="element">WPF-элемент, с которого читается ключ справки.</param>
    /// <returns>Строковый ключ или <see langword="null"/>, если не задан.</returns>
    public static string? GetHelpKey(DependencyObject element)
        => (string?)element.GetValue(HelpKeyProperty);

    /// <summary>
    /// Последний визуальный элемент, над которым находилась мышь.
    /// Используется как приоритетная точка поиска ключа справки при нажатии <c>F1</c>.
    /// </summary>
    private static DependencyObject? _lastHoverElement;

    /// <summary>
    /// Вызывается один раз в конструкторе окна: устанавливает обработчики MouseMove и F1.
    /// </summary>
    /// <param name="window">Окно WPF, в котором включается поддержка F1-справки.</param>
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
      LogInformation("F1-справка зарегистрирована для окна.");
    }

    /// <summary>
    /// Обход визуального деревах в поисках элемента с Tag типа <see cref="string"/>.
    /// </summary>
    /// <param name="element">Элемент, который нужно найти.</param>
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
    /// Формирует URL и открывает окно справки.
    /// </summary>
    /// <param name="pageName">Наименование страницы в помощи.</param>
    public static void ShowHelp(string pageName)
    {

      var helpDir = Path.Combine(
          AppDomain.CurrentDomain.BaseDirectory,
          "AppHelp");
      if (!Directory.Exists(helpDir))
      {
        LogError("Папка с содержимым контентом помощи не найденаю");
        return;
      }

      string url = string.IsNullOrWhiteSpace(pageName)
          ? "/index.html"
          : $"/index.html?cmd={Uri.EscapeDataString(pageName)}";

      LogInformation($"Путь до старницы: {url}");

      if (HelpViewerWindow._IsClose) HelpViewerWindow.LoadAndShow(url);
      else HelpViewerWindow.Load(url);
    }

    /// <summary>
    /// Открывает окно справки на странице быстрого меню команд.
    /// </summary>
    public static void OpenFastMenuCommand() 
    {
      LogInformation($"Путь до старницы: /FastMenuCommand.html");
      if (HelpViewerWindow._IsClose) HelpViewerWindow.LoadAndShow("/FastMenuCommand.html");
      else HelpViewerWindow.Load("/FastMenuCommand.html");
    }
  }
}