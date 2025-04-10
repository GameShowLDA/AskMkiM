using DataBaseConfiguration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Utilities.LoggerUtility;

namespace MainWindowProgram.HotkeyBindings
{
  /// <summary>
  /// Обеспечивает привязку горячих клавиш из базы данных к элементам меню.
  /// </summary>
  public static class MenuHotkeyBinder
  {
    /// <summary>
    /// Привязывает горячие клавиши к пунктам меню, основываясь на Tag и ActionName.
    /// </summary>
    /// <param name="window">Главное окно, в которое добавляются привязки.</param>
    /// <param name="dataContext">Контекст данных, содержащий команды.</param>
    /// <param name="context">Контекст базы данных с горячими клавишами.</param>
    public static void Attach(MainWindow window, object dataContext, AppDbContext context)
    {
      // Применяем шаблоны для меню, чтобы команды стали доступными
      if (window.mainMenu != null)
      {
        window.mainMenu.ApplyTemplate();

        foreach (var item in window.mainMenu.Items)
        {
          if (item is MenuItem mi)
            mi.ApplyTemplate();
        }
      }

      var allMenus = FindLogicalChildren<Menu>(window).ToList();

      foreach (var menu in allMenus)
      {
        LogInformation($"🍔 Меню найдено: Name={menu.Name}, Items.Count={menu.Items.Count}");
      }

      var hotkeys = context.FileHotKeys.ToList();
      var converter = new KeyGestureConverter();
      var menuItems = FindLogicalChildren<MenuItem>(window);

      LogInformation($"▶ Найдено {menuItems.Count()} пунктов меню для анализа горячих клавиш.");

      foreach (var menuItem in menuItems)
      {
        // Если Tag пустой или не строка, пропускаем элемент
        if (menuItem.Tag is not string actionName)
        {
          continue;
        }

        // Ищем горячую клавишу по ActionName
        var hotkey = hotkeys.FirstOrDefault(x => x.ActionName == actionName && x.IsEnabled);
        if (hotkey == null)
        {
          continue;
        }

        // Преобразуем строку комбинации в KeyGesture
        if (converter.ConvertFromString(hotkey.KeyCombination) is not KeyGesture gesture)
        {
          LogError($"❌ Ошибка преобразования комбинации клавиш: {hotkey.KeyCombination}");
          continue;
        }

        // Проверяем, что у menuItem есть Command, и если да — добавляем привязку
        if (menuItem.Command != null)
        {
          var binding = new KeyBinding(menuItem.Command, gesture);
          window.InputBindings.Add(binding); // Добавляем привязку в InputBindings

          // Устанавливаем InputGestureText
          menuItem.InputGestureText = hotkey.KeyCombination;

          LogInformation($"✅ Привязана горячая клавиша: {hotkey.KeyCombination} → {actionName}");
        }
        else
        {
          // Если команда не найдена — предупреждаем
          LogWarning($"⚠️ Команда не найдена для пункта меню: {actionName}");
        }
      }
    }

    /// <summary>
    /// Рекурсивно находит все элементы заданного типа в логическом дереве.
    /// </summary>
    private static IEnumerable<T> FindLogicalChildren<T>(DependencyObject depObj) where T : DependencyObject
    {
      if (depObj == null)
        yield break;

      foreach (var child in LogicalTreeHelper.GetChildren(depObj))
      {
        if (child is DependencyObject depChild)
        {
          if (depChild is T match)
            yield return match;

          foreach (var childOfChild in FindLogicalChildren<T>(depChild))
            yield return childOfChild;
        }
      }
    }
  }
}