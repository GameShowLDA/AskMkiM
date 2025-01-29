using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Utilities.LoggerUtility;

namespace MainWindowProgram
{
  public partial class MainWindow
  {
    public static readonly RoutedUICommand ActivateMenuItemCommand = new RoutedUICommand(
    "ActivateMenuItem", "ActivateMenuItemCommand", typeof(MainWindow));
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

    private void ExecuteMenuItemAction(MenuItem menuItem)
    {
      switch (menuItem.Name)
      {
        // Файл
        case "OpenMenuItem":
          Open_PreviewMouseDownAsync(menuItem, null);
          break;
        case "CreateMenuItem":
          Create_PreviewMouseDownAsync(menuItem, null);
          break;
        case "ExitMenuItem":
          Exit_Handler(menuItem, null);
          break;

        // Метрология
        case "ModeKSMenuItem":
          KC_Handler(menuItem, null);
          break;
        case "ModeIEMenuItem":
          IE_Handler(menuItem, null);
          break;
        case "ModeCIMenuItem":
          CI_Handler(menuItem, null);
          break;

        // Самоконтроль
        case "SelfControlMenuItem":
          Test_Self(menuItem, null);
          break;

        // Тесты
        case "NodeMethodMenuItem":
          NodeMethodControl_PreviewMouseDown(menuItem, null);
          break;

        // Настройки
        case "ExecutionMenuItem":
          Execution_Handler(menuItem, null);
          break;
        case "ConfigMenuItem":
          Config_Handler(menuItem, null);
          break;
        case "ErrorMenuItem":
          Error_Handler(menuItem, null);
          break;
        case "ProtocolMenuItem":
          Protocol_Handler(menuItem, null);
          break;

        // Администрирование
        case "USBMenuItem":
          USB_Handler(menuItem, null);
          break;
        case "SendCommandMenuItem":
          SendCommand_Handler(menuItem, null);
          break;
        case "LoggerMenuItem":
          Logger_Handler(menuItem, null);
          break;

        default:
          LogWarning($"Неизвестный элемент меню: {menuItem.Name}");
          break;
      }
    }

    private async Task HandleUserControlPreviewMouseDownAsync(object sender, MouseButtonEventArgs e)
    {
      if (sender is UserControl userControl)
      {
        switch (userControl.Name)
        {
          case "PowerButton":
            await PowerButton.PowerButtonClick();
            break;
          default:
            LogWarning($"Неизвестный UserControl: {userControl.Name}");
            break;
        }

        e.Handled = true;
      }
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        if (Keyboard.FocusedElement is MenuItem focusedMenuItem)
        {
          ExecuteMenuItemAction(focusedMenuItem);
          LogInformation($"Menu item '{focusedMenuItem.Header}' activated with Enter key.");
        }
      }
    }

    private void RegisterHotkeys()
    {
      RegisterHotkey(Key.D1, ModifierKeys.Control, () => FocusMenuItem(File));
      RegisterHotkey(Key.D2, ModifierKeys.Control, () => FocusMenuItem(Metrology));
      RegisterHotkey(Key.D3, ModifierKeys.Control, () => FocusMenuItem(SelfControl));
      RegisterHotkey(Key.D4, ModifierKeys.Control, () => FocusMenuItem(TestControl));
      RegisterHotkey(Key.D5, ModifierKeys.Control, () => FocusMenuItem(Settings));
      RegisterHotkey(Key.D6, ModifierKeys.Control, () => FocusMenuItem(Help));
      RegisterHotkey(Key.D7, ModifierKeys.Control, () => FocusMenuItem(Admin));

      RegisterHotkey(Key.P, ModifierKeys.Control, async () => await SimulateButtonAsync(PowerButton));
    }

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

    private void ExecuteActivateMenuItem(object sender, ExecutedRoutedEventArgs e)
    {
      if (e.Parameter is MenuItem menuItem)
      {
        menuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        LogInformation($"Menu item '{menuItem.Header}' activated with Enter key.");
      }
    }

    private async Task SimulateButtonAsync(UserControl userControl)
    {
      var mouseEventArgs = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
      {
        RoutedEvent = UIElement.MouseLeftButtonDownEvent,
        Source = userControl
      };

      await HandleUserControlPreviewMouseDownAsync(PowerButton, mouseEventArgs);
    }
  }
}
