using Ask.Core.Shared.DTO.Settings;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using static Ask.LogLib.LoggerUtility;

namespace MainWindowProgram.HotkeyBindings
{
  /// <summary>
  /// Обеспечивает привязку горячих клавиш из базы данных к элементам меню и поддерживает иерархическую навигацию по Ctrl+X.
  /// </summary>
  public static class MenuHotkeyBinder
  {
    private static MenuItem? _currentOpenMenu;
    private static string? _currentUidPrefix;

    /// <summary>
    /// Привязывает горячие клавиши к пунктам меню, основываясь на Tag и ActionName.
    /// </summary>
    public static void Attach(MainWindow window, object dataContext, IReadOnlyCollection<FileHotkeyDto> hotkeys)
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

      var converter = new KeyGestureConverter();
      var menuItems = FindLogicalChildren<MenuItem>(window).ToList();

      LogInformation($"▶ Найдено {menuItems.Count} пунктов меню для анализа горячих клавиш.");

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

      ComponentDispatcher.ThreadPreprocessMessage += (ref MSG msg, ref bool handled) =>
      {
        ProcessHierarchyKeyCombination(menuItems, msg, ref handled);
      };
    }

    /// <summary>
    /// Обрабатывает Ctrl+Shift+N и Ctrl+N по иерархии Uid.
    /// </summary>
    private static void ProcessHierarchyKeyCombination(List<MenuItem> allItems, MSG msg, ref bool handled)
    {
      const int WM_KEYDOWN = 0x0100;
      const int WM_SYSKEYDOWN = 0x0104;

      if (msg.message != WM_KEYDOWN && msg.message != WM_SYSKEYDOWN)
        return;

      var key = KeyInterop.KeyFromVirtualKey((int)msg.wParam);

      if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
      {
        if (key >= Key.D1 && key <= Key.D9)
        {
          string uid = (key - Key.D0).ToString();
          var target = GetMenuItemByUid(allItems, uid);
          if (target != null)
          {
            CloseAllMenus(allItems);
            target.IsSubmenuOpen = true;
            target.Focus();
            _currentOpenMenu = target;
            _currentUidPrefix = uid;
            handled = true;

            LogInformation($"📂 Открыт верхний пункт меню с Uid={uid}");
          }
        }
      }
      else if (Keyboard.Modifiers == ModifierKeys.Control && _currentOpenMenu != null)
      {
        if (key >= Key.D1 && key <= Key.D9)
        {
          string uid = $"{_currentUidPrefix}.{key - Key.D0}";
          var children = _currentOpenMenu.Items.OfType<MenuItem>().ToList();
          var child = GetMenuItemByUid(children, uid);

          if (child != null)
          {
            TryExecuteOrOpen(allItems, child);
            handled = true;
          }
        }
      }
    }

    /// <summary>
    /// Пытается выполнить команду меню или открыть подменю.
    /// </summary>
    private static void TryExecuteOrOpen(List<MenuItem> allItems, MenuItem menuItem)
    {
      if (menuItem.HasItems)
      {
        menuItem.IsSubmenuOpen = true;
        menuItem.Focus();
        _currentOpenMenu = menuItem;
        _currentUidPrefix = menuItem.Uid;

        LogInformation($"📂 Открыт вложенный пункт меню с Uid={menuItem.Uid}");
      }
      else if (menuItem.Command != null && menuItem.Command.CanExecute(null))
      {
        menuItem.Command.Execute(null);
        ResetMenuState();
        CloseAllMenus(allItems);

        LogInformation($"✅ Выполнена команда из пункта меню с Uid={menuItem.Uid}");
      }
    }

    /// <summary>
    /// Сброс текущей иерархии.
    /// </summary>
    private static void ResetMenuState()
    {
      _currentOpenMenu = null;
      _currentUidPrefix = null;
    }

    /// <summary>
    /// Находит MenuItem с указанным Uid.
    /// </summary>
    private static MenuItem? GetMenuItemByUid(IEnumerable<MenuItem> items, string uid)
    {
      return items.FirstOrDefault(m => m.Uid == uid);
    }

    /// <summary>
    /// Привязывает Ctrl+Shift+N к раскрытию верхнего уровня по Tag.
    /// </summary>
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
                var parentMenu = FindParent<Menu>(menuItem);
                parentMenu?.Focus();
                menuItem.Focus();
                menuItem.IsSubmenuOpen = true;
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
    /// Закрывает все раскрытые меню.
    /// </summary>
    private static void CloseAllMenus(IEnumerable<MenuItem> items)
    {
      foreach (var item in items)
        item.IsSubmenuOpen = false;
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

    public static void RenumberVisibleMenuItemsWithUid(MenuItem menuItem, string uidPrefix)
    {
      if (!menuItem.HasItems)
        return;

      int index = 1;

      foreach (var item in menuItem.Items.OfType<MenuItem>())
      {
        if (item is not MenuItem mi)
          continue;

        if (mi.Visibility == Visibility.Visible && index <= 9)
        {
          string baseText = StripHeaderText(mi.Header?.ToString());
          mi.Header = $"{index}. {baseText}";
          mi.Uid = $"{uidPrefix}.{index}";

          if (mi.HasItems)
            RenumberVisibleMenuItemsWithUid(mi, mi.Uid);

          index++;
        }
        else
        {
          mi.Uid = string.Empty;
          mi.Header = StripHeaderText(mi.Header?.ToString());

          if (mi.HasItems)
            RenumberVisibleMenuItemsWithUid(mi, string.Empty);
        }
      }
    }

    /// <summary>
    /// Убирает нумерацию (например, "1. Архив" → "Архив").
    /// </summary>
    private static string StripHeaderText(string? header)
    {
      if (string.IsNullOrWhiteSpace(header))
        return string.Empty;

      var dotIndex = header.IndexOf('.');
      if (dotIndex >= 0 && dotIndex + 1 < header.Length)
        return header.Substring(dotIndex + 1).Trim();

      return header.Trim();
    }

    /// <summary>
    /// Подключает автоматическую нумерацию всех верхних пунктов меню при открытии.
    /// </summary>
    /// <param name="menu">Главное меню, например mainMenu.</param>
    public static void BindAutoRenumbering(Menu menu)
    {
      var topLevelItems = menu.Items.OfType<MenuItem>().ToList();

      for (int i = 0; i < topLevelItems.Count; i++)
      {
        var topMenuItem = topLevelItems[i];
        string uidPrefix = (i + 1).ToString();

        topMenuItem.Uid = uidPrefix;

        topMenuItem.SubmenuOpened += (_, _) =>
        {
          MenuHotkeyBinder.RenumberVisibleMenuItemsWithUid(topMenuItem, uidPrefix);
        };
      }
    }
  }
}
