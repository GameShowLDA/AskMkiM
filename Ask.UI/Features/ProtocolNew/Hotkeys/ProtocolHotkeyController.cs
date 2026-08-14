using Ask.UI.Infrastructure.UI.Overlay.Drawer.Runtime;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Features.ProtocolNew.Hotkeys
{
  /// <summary>
  /// Маршрутизирует горячие клавиши исполнительного протокола в операции интерфейса.
  /// Не выполняет команды самостоятельно и не зависит от конкретных кнопок WPF.
  /// </summary>
  internal sealed class ProtocolHotkeyController
  {
    /// <summary>Контекст состояния и команд исполнительного интерфейса.</summary>
    private readonly IProtocolHotkeyContext _context;

    /// <summary>
    /// Создаёт контроллер горячих клавиш.
    /// </summary>
    /// <param name="context">Контекст состояния и команд интерфейса.</param>
    public ProtocolHotkeyController(IProtocolHotkeyContext context)
    {
      _context = context;
    }

    /// <summary>
    /// Обрабатывает нажатие клавиши и вызывает разрешённую для текущего состояния команду.
    /// </summary>
    /// <param name="sender">Источник события клавиатуры.</param>
    /// <param name="e">Аргументы события клавиатуры.</param>
    public void HandleKeyDown(object sender, KeyEventArgs e)
    {
      var key = e.Key == Key.System ? e.SystemKey : e.Key;
      var modifiers = Keyboard.Modifiers;
      var drawerBlocksInput = DrawerHostService.Instance.ShouldBlockGlobalInput;
      var textInputFocused = IsTextInputFocused();

      if (key == Key.F5)
      {
        LogInformation(
          $"[PauseTiming] F5 KeyDown: context={RuntimeHelpers.GetHashCode(_context)}, " +
          $"thread={Environment.CurrentManagedThreadId}, modifiers={modifiers}, " +
          $"drawerBlocked={drawerBlocksInput}, textInputFocused={textInputFocused}, " +
          $"canStart={_context.CanStart}, canPause={_context.CanPause}, canContinue={_context.CanContinue}");
      }

      if (drawerBlocksInput
          || (textInputFocused && !CanHandleWhileTextInputFocused(key, modifiers)))
      {
        return;
      }

      if (modifiers != ModifierKeys.None)
      {
        NotifySpecialKey(sender, e, key);
        return;
      }

      switch (key)
      {
        case Key.Enter when _context.CanStart:
          _context.Start();
          e.Handled = true;
          break;
        case Key.F5:
          LogInformation(
            $"[PauseTiming] F5 routed: context={RuntimeHelpers.GetHashCode(_context)}, " +
            $"action={GetF5Action()}");
          _context.RunOrPause();
          e.Handled = true;
          break;
        case Key.F10:
          _context.Step(isStepInto: false);
          e.Handled = true;
          break;
        case Key.F11:
          _context.Step(isStepInto: true);
          e.Handled = true;
          break;
        case Key.F4 when _context.CanJumpToCommand:
          _context.JumpToCommand();
          e.Handled = true;
          break;
        case Key.P when _context.CanContinue:
          _context.Continue();
          e.Handled = true;
          break;
        case Key.P when _context.CanPause:
          _context.Pause();
          e.Handled = true;
          break;
        case Key.P:
          e.Handled = true;
          break;
        case Key.Escape when _context.CanExit:
          _context.Exit();
          e.Handled = true;
          break;
        case Key.R when _context.CanRepeat:
          _context.Repeat();
          e.Handled = true;
          break;
        default:
          NotifySpecialKey(sender, e, key);
          break;
      }
    }

    /// <summary>
    /// Проверяет доступность команды выполнения при сохранённом фокусе в поле ввода.
    /// </summary>
    /// <param name="key">Нажатая клавиша.</param>
    /// <param name="modifiers">Активные клавиши-модификаторы.</param>
    /// <returns>
    /// <see langword="true"/>, если клавиша соответствует доступной команде выполнения.
    /// В противном случае — <see langword="false"/>.
    /// </returns>
    private bool CanHandleWhileTextInputFocused(Key key, ModifierKeys modifiers)
    {
      if (modifiers != ModifierKeys.None)
      {
        return false;
      }

      return key switch
      {
        Key.P => _context.CanContinue || _context.CanPause,
        Key.Escape => _context.CanExit,
        Key.R => _context.CanRepeat,
        _ => false
      };
    }

    /// <summary>Проверяет, находится ли фокус в элементе ввода текста.</summary>
    private static bool IsTextInputFocused()
    {
      return Keyboard.FocusedElement is TextBox or PasswordBox or ComboBox;
    }

    /// <summary>
    /// Определяет действие, назначенное клавише F5 для текущего состояния выполнения.
    /// </summary>
    /// <returns>Имя действия, назначенного клавише F5.</returns>
    private string GetF5Action()
    {
      if (_context.CanStart)
      {
        return "Start";
      }

      if (_context.CanContinue)
      {
        return "Continue";
      }

      if (_context.CanPause)
      {
        return "Pause";
      }

      return "None";
    }

    /// <summary>Передаёт Alt существующему внешнему обработчику клавиатуры.</summary>
    private void NotifySpecialKey(object sender, KeyEventArgs e, Key key)
    {
      if (key == Key.LeftAlt || key == Key.RightAlt)
      {
        _context.NotifyOtherKey(sender, e);
      }
    }
  }
}
