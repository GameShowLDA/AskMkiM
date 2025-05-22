using DataBaseConfiguration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
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
        if (menuItem.Tag is not string actionName)
        {
          continue;
        }

        var hotkey = hotkeys.FirstOrDefault(x => x.ActionName == actionName && x.IsEnabled);
        if (hotkey == null)
        {
          BindHotkeyToMenuItem(window, menuItem);
          continue;
        }

        if (converter.ConvertFromString(hotkey.KeyCombination) is not KeyGesture gesture)
        {
          LogError($"❌ Ошибка преобразования комбинации клавиш: {hotkey.KeyCombination}");
          continue;
        }

        if (menuItem.Command != null)
        {
          var binding = new KeyBinding(menuItem.Command, gesture);
          window.InputBindings.Add(binding);
          menuItem.InputGestureText = hotkey.KeyCombination;

          LogInformation($"✅ Привязана горячая клавиша: {hotkey.KeyCombination} → {actionName}");
        }
        else
        {
          LogWarning($"⚠️ Команда не найдена для пункта меню: {actionName}");
        }
      }
    }

    /// <summary>
    /// Глобально привязывает горячую клавишу Ctrl+Shift+[номер] к пункту верхнего меню.
    /// При нажатии горячей клавиши меню раскрывается и подсвечивается, как при нажатии Alt.
    /// </summary>
    /// <param name="window">Главное окно приложения.</param>
    /// <param name="menuItem">Пункт меню верхнего уровня, к которому привязывается горячая клавиша.</param>
    private static void BindHotkeyToMenuItem(MainWindow window, MenuItem menuItem)
    {
      if (menuItem.Tag is not string tag)
      {
        LogDebug($"Tag отсутствует или не является строкой у {menuItem.Name}");
        return;
      }

      var tags = tag.Split('_');

      if (tags.Length == 2 && tags[0] == "menuHeader" && int.TryParse(tags[1], out int menuNumber))
      {
        var key = Key.D0 + menuNumber;

        LogDebug($"Привязываем Ctrl+Shift+{menuNumber} ({key}) к {menuItem.Name}");

        ComponentDispatcher.ThreadPreprocessMessage += (ref MSG msg, ref bool handled) =>
        {
          if (handled)
            return;

          const int WM_KEYDOWN = 0x0100;
          const int WM_SYSKEYDOWN = 0x0104;

          if (msg.message != WM_KEYDOWN && msg.message != WM_SYSKEYDOWN)
            return;

          if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
          {
            Key pressedKey = KeyInterop.KeyFromVirtualKey((int)msg.wParam);
            if (pressedKey == key)
            {
              LogDebug($"Открываем и подсвечиваем меню: {menuItem.Name}");

              menuItem.Dispatcher.Invoke(() =>
              {
                // Найдём родительский Menu, чтобы отдать фокус
                var parentMenu = FindParent<Menu>(menuItem);
                parentMenu?.Focus(); // Фокус на всё меню

                menuItem.Focus();    // Фокус на конкретный пункт
                menuItem.IsSubmenuOpen = true; // Открываем
              });

              handled = true;
            }
          }
        };
      }
      else
      {
        LogDebug($"Тег {tag} не соответствует шаблону menuHeader_X");
      }
    }

    /// <summary>
    /// Поиск родителя нужного типа в визуальном дереве.
    /// </summary>
    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
      DependencyObject? parent = VisualTreeHelper.GetParent(child);
      while (parent != null && parent is not T)
      {
        parent = VisualTreeHelper.GetParent(parent);
      }
      return parent as T;
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