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
      // Проверяем, что menu доступно
      if (window.mainMenu != null)
      {
        // Применяем шаблоны для меню
        window.mainMenu.ApplyTemplate();

        foreach (var item in window.mainMenu.Items)
        {
          if (item is MenuItem mi)
            mi.ApplyTemplate(); // Применяем шаблон для каждого MenuItem
        }
      }

      foreach (var cb in window.CommandBindings)
      {
        if (cb is CommandBinding commandBinding)
        {
          LogDebug($"🔧 Window.CommandBinding: {commandBinding.Command}");
        }
      }


      // Получаем все Menu элементы из логического дерева
      var allMenus = FindLogicalChildren<Menu>(window).ToList();

      // Логируем все меню
      foreach (var menu in allMenus)
      {
        LogInformation($"🍔 Меню найдено: Name={menu.Name}, Items.Count={menu.Items.Count}");
      }

      // Получаем список горячих клавиш из базы данных
      var hotkeys = context.FileHotKeys.ToList();
      var converter = new KeyGestureConverter();

      // Получаем все MenuItem из логического дерева
      var menuItems = FindLogicalChildren<MenuItem>(window);

      LogInformation($"▶ Найдено {menuItems.Count()} пунктов меню для анализа горячих клавиш.");

      // Обрабатываем все MenuItem
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
          continue;
        }

        // Проверяем, что у menuItem есть Command, и если да — добавляем привязку
        if (menuItem.Command != null)
        {
          var binding = new KeyBinding(menuItem.Command, gesture);
          window.InputBindings.Add(binding); // Добавляем привязку в InputBindings

          // Отображаем сочетание клавиш рядом с меню
          menuItem.InputGestureText = hotkey.KeyCombination;

          // Логируем успешную привязку
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