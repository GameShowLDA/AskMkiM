using AppConfiguration.Base;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.Components;
using UI.Controls.TextEditor;
using static Utilities.LoggerUtility;

namespace MainWindowProgram
{
  /// <summary>
  /// Основное окно программы.
  /// </summary>
  public partial class MainWindow
  {
    private bool isSearchWindowOpened;
    public bool IsTextEditorActive { get; set;}

    /// <summary>
    /// Обработчик события KeyDown для окна.
    /// При нажатии клавиши Enter, если элемент с фокусом является MenuItem,
    /// генерируется событие клика, и логируется информация.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события клавиатуры.</param>
    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        if (Keyboard.FocusedElement is MenuItem focusedMenuItem)
        {
          focusedMenuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
          LogInformation($"Menu item '{focusedMenuItem.Header}' activated with Enter key.");
          e.Handled = true;
        }
      }
    }

    /// <summary>
    /// Обработчик события нажатия мыши для элементов управления UserControl.
    /// В зависимости от имени UserControl вызывается соответствующий асинхронный метод.
    /// </summary>
    /// <param name="sender">Источник события (UserControl).</param>
    /// <param name="e">Аргументы события мыши.</param>
    private async Task HandleUserControlPreviewMouseDownAsync(object sender, MouseButtonEventArgs e)
    {
      if (sender is Control userControl)
      {
        switch (userControl.Name)
        {
          case "PowerButton":
            await PowerButton.PowerButtonClick();
            break;

          case "searchMenuItem":
            SearchMenuItem_PreviewMouseDown(sender, e);
            break;

          default:
            LogWarning($"Неизвестный UserControl: {userControl.Name}");
            break;
        }

        e.Handled = true;
      }
    }

    /// <summary>
    /// Регистрирует горячие клавиши для быстрого доступа к пунктам меню и кнопкам.
    /// Каждая горячая клавиша настраивается с использованием комбинации клавиш и модификаторов.
    /// </summary>
    private void RegisterHotkeys()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        RegisterHotkey(Key.D1, ModifierKeys.Control, () => FocusMenuItem(File));
        RegisterHotkey(Key.D2, ModifierKeys.Control, () => FocusMenuItem(Metrology));
        RegisterHotkey(Key.D3, ModifierKeys.Control, () => FocusMenuItem(SelfControl));
        RegisterHotkey(Key.D4, ModifierKeys.Control, () => FocusMenuItem(TestControl));
        RegisterHotkey(Key.D5, ModifierKeys.Control, () => FocusMenuItem(Settings));
        RegisterHotkey(Key.D7, ModifierKeys.Control, () => FocusMenuItem(Admin));
        RegisterHotkey(Key.P, ModifierKeys.Control, async () => await SimulateButtonAsync(PowerButton));
        RegisterHotkey(Key.F, ModifierKeys.Control, async () => await ShowSearchWindow(searchMenuItem));
      });
    }

    /// <summary>
    /// Регистрирует отдельную горячую клавишу с заданным сочетанием клавиш и действием.
    /// Создаётся KeyGesture и привязывается действие, которое выполняется при срабатывании горячей клавиши.
    /// </summary>
    /// <param name="key">Клавиша, которая используется для горячей клавиши.</param>
    /// <param name="modifiers">Модификаторы для горячей клавиши.</param>
    /// <param name="action">Действие, которое должно выполниться.</param>
    private void RegisterHotkey(Key key, ModifierKeys modifiers, Action action)
    {
      var gesture = new KeyGesture(key, modifiers);
      var command = new RoutedCommand();
      command.InputGestures.Add(gesture);
      this.CommandBindings.Add(new CommandBinding(command, (sender, e) =>
      {
        LogInformation($"Горячая клавиша {modifiers} + {key} сработала.");
        action();
      }));
    }

    /// <summary>
    /// Фокусирует и открывает указанный пункт меню.
    /// Если у меню есть подменю, фокус переходит на первый пункт подменю.
    /// Логируется информация о текущем фокусе.
    /// </summary>
    /// <param name="menuItem">Пункт меню, который требуется открыть и сфокусировать.</param>
    private void FocusMenuItem(MenuItem menuItem)
    {
      menuItem.IsSubmenuOpen = true;
      menuItem.Focus();
      LogInformation($"Меню '{menuItem.Header}' открыто и сфокусировано.");

      if (menuItem.Items.Count > 0 && menuItem.Items[0] is MenuItem firstSubMenuItem)
      {
        firstSubMenuItem.Focus();
        LogInformation($"Подменю '{firstSubMenuItem.Header}' сфокусировано.");
      }

      var focusedElement = Keyboard.FocusedElement;
      LogInformation($"Текущий фокус: {focusedElement}");
    }

    /// <summary>
    /// Выполняет команду активации пункта меню при срабатывании команды.
    /// Генерирует событие клика для пункта меню.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы выполненной команды.</param>
    private void ExecuteActivateMenuItem(object sender, ExecutedRoutedEventArgs e)
    {
      if (e.Parameter is MenuItem menuItem)
      {
        menuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        LogInformation($"Menu item '{menuItem.Header}' activated with Enter key.");
      }
    }

    /// <summary>
    /// Симулирует нажатие на кнопку на элементе управления UserControl.
    /// Создаёт событие нажатия мыши и вызывает обработчик события для данного элемента.
    /// </summary>
    /// <param name="userControl">Элемент управления, на котором симулируется нажатие.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    private async Task SimulateButtonAsync(UserControl userControl)
    {
      var mouseEventArgs = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
      {
        RoutedEvent = UIElement.MouseLeftButtonDownEvent,
        Source = userControl,
      };

      await HandleUserControlPreviewMouseDownAsync(PowerButton, mouseEventArgs);
    }

    private async Task ShowSearchWindow(MenuItem userControl)
    {
      if (IsTextEditorActive)
      {
        var mouseEventArgs = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
        {
          RoutedEvent = UIElement.MouseLeftButtonDownEvent,
          Source = userControl,
        };
        if (!_isSearchWindowOpen)
        {
          await HandleUserControlPreviewMouseDownAsync(searchMenuItem, mouseEventArgs);
        }
        else
        {
          TextEditorUI activeEditor = MultiWindow.GetActiveTextEditor();
          string selectedText = activeEditor?.TextArea.Selection.GetText();
          if (!string.IsNullOrEmpty(selectedText))
          {
            var foundElement = _searchWindow.FindName("SearchTextBox");
            if (foundElement != null)
            {
              var foundTextBox = foundElement as TextBox;
              foundTextBox.Text = selectedText;
            }
          }
        }
      }
    }
  }
}
