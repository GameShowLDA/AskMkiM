using Ask.Core.Shared.DTO.Settings;
using Ask.UI.Infrastructure.UI.Overlay.Drawer.Runtime;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Ask.LogLib.LoggerUtility;

namespace MainWindowProgram.HotkeyBindings
{
  /// <summary>
  /// Универсальный обработчик хоткеев: связывает комбинации из БД с элементами по Tag и исполняет их
  /// через ICommand или программный Click, если команды нет.
  /// </summary>
  public static class UniversalHotkeyBinder
  {
    private static readonly Dictionary<string, List<Func<bool>>> Map = new(StringComparer.OrdinalIgnoreCase);
    private static bool _attached;

    public static void Attach(MainWindow window, IReadOnlyCollection<FileHotkeyDto> hotkeys)
    {
      Map.Clear();

      var records = hotkeys.Where(x => x.IsEnabled).ToList();
      var elements = FindLogicalChildren<FrameworkElement>(window).ToList();
      var occupiedCombos = GetExistingInputBindingCombos(window);

      foreach (var element in elements)
      {
        if (element.Tag is not string actionName)
          continue;

        var rec = records.FirstOrDefault(r => string.Equals(r.ActionName, actionName, StringComparison.Ordinal));
        if (rec is null)
          continue;

        if (!TryParseCombo(rec.KeyCombination, out var key, out var mods))
        {
          LogWarning($"Комбинация \"{rec.KeyCombination}\" не распознана и будет пропущена.");
          continue;
        }

        var normalized = NormalizeCombo(key, mods);
        if (occupiedCombos.Contains(normalized))
          continue;

        if (!TryCreateInvoker(element, out var invoker))
        {
          LogWarning($"Элемент {element.GetType().Name} с Tag={actionName} не поддерживается ни как ICommandSource, ни как кликабельный контрол.");
          continue;
        }

        SetHotkeyTooltip(element, normalized, element is MenuItem);

        if (!Map.TryGetValue(normalized, out var list))
        {
          list = new List<Func<bool>>();
          Map[normalized] = list;
        }

        list.Add(invoker);
      }

      if (!_attached)
      {
        window.PreviewKeyDown += OnPreviewKeyDown;
        _attached = true;
      }
    }

    private static void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
      if (sender is not Window)
        return;

      var key = e.Key == Key.System ? e.SystemKey : e.Key;
      if (DrawerHostService.Instance.ShouldBlockGlobalInput)
      {
        return;
      }

      var mods = Keyboard.Modifiers;
      key = UnifyNumpadDigits(key);

      var combo = NormalizeCombo(key, mods);
      if (!Map.TryGetValue(combo, out var actions) || actions.Count == 0)
        return;

      foreach (var act in actions)
      {
        try
        {
          if (act())
          {
            e.Handled = true;
            return;
          }
        }
        catch (Exception ex)
        {
          LogWarning($"Ошибка выполнения действия по хоткею {combo}: {ex.Message}");
        }
      }
    }

    private static bool TryCreateInvoker(FrameworkElement element, out Func<bool> invoker)
    {
      invoker = null!;

      if (element is ICommandSource cmdSrc)
      {
        invoker = () =>
        {
          var cmd = cmdSrc.Command;
          var param = cmdSrc.CommandParameter;
          if (cmd == null) return false;
          if (!cmd.CanExecute(param)) return false;
          cmd.Execute(param);
          return true;
        };
        return true;
      }

      var typeName = element.GetType().FullName ?? string.Empty;
      if (typeName == "UI.Components.PowerButton")
      {
        invoker = () =>
        {
          try
          {
            var m = element.GetType().GetMethod("PowerButtonClick",
              System.Reflection.BindingFlags.Instance |
              System.Reflection.BindingFlags.Public |
              System.Reflection.BindingFlags.NonPublic);

            if (m == null)
              return false;

            if (typeof(Task).IsAssignableFrom(m.ReturnType))
            {
              element.Dispatcher.InvokeAsync(async () =>
              {
                var task = (Task?)m.Invoke(element, null);
                if (task != null) await task;
              });
            }
            else
            {
              element.Dispatcher.Invoke(() => m.Invoke(element, null));
            }
            return true;
          }
          catch
          {
            return false;
          }
        };
        return true;
      }

      var candidateNames = new[] { "PowerButtonClick", "Click", "OnClick", "PerformClick", "Invoke", "Execute" };
      var mi = candidateNames
        .Select(name => element.GetType().GetMethod(name,
          System.Reflection.BindingFlags.Instance |
          System.Reflection.BindingFlags.Public |
          System.Reflection.BindingFlags.NonPublic,
          binder: null,
          types: Type.EmptyTypes,
          modifiers: null))
        .FirstOrDefault(m => m != null);

      if (mi != null)
      {
        invoker = () =>
        {
          try
          {
            if (typeof(Task).IsAssignableFrom(mi.ReturnType))
            {
              element.Dispatcher.InvokeAsync(async () =>
              {
                var task = (Task?)mi.Invoke(element, null);
                if (task != null) await task;
              });
            }
            else
            {
              element.Dispatcher.Invoke(() => mi.Invoke(element, null));
            }
            return true;
          }
          catch
          {
            return false;
          }
        };
        return true;
      }

      invoker = null!;
      return false;
    }

    private static bool TryParseCombo(string text, out Key key, out ModifierKeys mods)
    {
      key = Key.None;
      mods = ModifierKeys.None;

      if (string.IsNullOrWhiteSpace(text))
        return false;

      var parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
      if (parts.Length == 0)
        return false;

      foreach (var part in parts)
      {
        var p = part.ToLowerInvariant();
        if (p is "ctrl" or "control") { mods |= ModifierKeys.Control; continue; }
        if (p is "alt") { mods |= ModifierKeys.Alt; continue; }
        if (p is "shift") { mods |= ModifierKeys.Shift; continue; }

        if (TryParseKeyToken(part, out var k)) { key = k; continue; }
        return false;
      }

      if (key == Key.None) return false;
      key = UnifyNumpadDigits(key);
      return true;
    }

    private static string NormalizeCombo(Key key, ModifierKeys mods)
    {
      var parts = new List<string>(4);
      if ((mods & ModifierKeys.Control) != 0) parts.Add("Ctrl");
      if ((mods & ModifierKeys.Alt) != 0) parts.Add("Alt");
      if ((mods & ModifierKeys.Shift) != 0) parts.Add("Shift");
      parts.Add(KeyToToken(key));
      return string.Join("+", parts);
    }

    private static HashSet<string> GetExistingInputBindingCombos(Window window)
    {
      var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (var ib in window.InputBindings.OfType<KeyBinding>())
      {
        if (ib.Gesture is KeyGesture kg)
        {
          var k = UnifyNumpadDigits(kg.Key);
          set.Add(NormalizeCombo(k, kg.Modifiers));
        }
      }
      return set;
    }

    private static Key UnifyNumpadDigits(Key key) => key switch
    {
      Key.NumPad0 => Key.D0,
      Key.NumPad1 => Key.D1,
      Key.NumPad2 => Key.D2,
      Key.NumPad3 => Key.D3,
      Key.NumPad4 => Key.D4,
      Key.NumPad5 => Key.D5,
      Key.NumPad6 => Key.D6,
      Key.NumPad7 => Key.D7,
      Key.NumPad8 => Key.D8,
      Key.NumPad9 => Key.D9,
      _ => key
    };

    private static bool TryParseKeyToken(string token, out Key key)
    {
      key = Key.None;
      var t = token.Trim();

      if (t.Length == 1)
      {
        var ch = char.ToUpperInvariant(t[0]);
        if (ch is >= 'A' and <= 'Z') { key = Key.A + (ch - 'A'); return true; }
        if (ch is >= '0' and <= '9') { key = Key.D0 + (ch - '0'); return true; }
      }

      if (t.StartsWith("F", true, CultureInfo.InvariantCulture) && int.TryParse(t[1..], out var fn) && fn is >= 1 and <= 24)
      {
        key = Key.F1 + (fn - 1);
        return true;
      }

      return t.ToLowerInvariant() switch
      {
        "enter" or "return" => (key = Key.Enter) != Key.None,
        "esc" or "escape" => (key = Key.Escape) != Key.None,
        "tab" => (key = Key.Tab) != Key.None,
        "space" or "spacebar" => (key = Key.Space) != Key.None,
        "back" or "backspace" => (key = Key.Back) != Key.None,
        "del" or "delete" => (key = Key.Delete) != Key.None,
        "ins" or "insert" => (key = Key.Insert) != Key.None,
        "home" => (key = Key.Home) != Key.None,
        "end" => (key = Key.End) != Key.None,
        "pgup" or "pageup" => (key = Key.PageUp) != Key.None,
        "pgdown" or "pgdn" or "pagedown" => (key = Key.PageDown) != Key.None,
        "left" => (key = Key.Left) != Key.None,
        "right" => (key = Key.Right) != Key.None,
        "up" => (key = Key.Up) != Key.None,
        "down" => (key = Key.Down) != Key.None,
        "oemplus" or "+" => (key = Key.OemPlus) != Key.None,
        "oemminus" or "-" => (key = Key.OemMinus) != Key.None,
        _ => false
      };
    }

    private static string KeyToToken(Key key)
    {
      if (key is >= Key.A and <= Key.Z) return key.ToString();
      if (key is >= Key.D0 and <= Key.D9) return ((int)key - (int)Key.D0).ToString(CultureInfo.InvariantCulture);
      if (key is >= Key.F1 and <= Key.F24) return $"F{(int)key - (int)Key.F1 + 1}";

      return key switch
      {
        Key.Enter or Key.Return => "Enter",
        Key.Escape => "Esc",
        Key.Tab => "Tab",
        Key.Space => "Space",
        Key.Back => "Backspace",
        Key.Delete => "Delete",
        Key.Insert => "Insert",
        Key.Home => "Home",
        Key.End => "End",
        Key.PageUp => "PageUp",
        Key.PageDown => "PageDown",
        Key.Left => "Left",
        Key.Right => "Right",
        Key.Up => "Up",
        Key.Down => "Down",
        Key.OemPlus => "+",
        Key.OemMinus => "-",
        _ => key.ToString()
      };
    }

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

    private static void SetHotkeyTooltip(FrameworkElement element, string combo, bool isMenuItem)
    {
      if (isMenuItem)
      {
        return;
      }

      MergeTooltip(element, combo);
    }

    private static void MergeTooltip(FrameworkElement element, string combo)
    {
      string? baseText = TryGetTooltipText(element);
      string newTip;

      if (string.IsNullOrWhiteSpace(baseText))
      {
        newTip = combo;
      }
      else
      {
        if (baseText.Contains(combo, StringComparison.OrdinalIgnoreCase))
          return;

        newTip = $"{baseText} — {combo}";
      }

      element.ToolTip = newTip;
    }

    private static string? TryGetTooltipText(FrameworkElement element)
    {
      switch (element.ToolTip)
      {
        case null:
          return null;
        case string s:
          return s;
        case TextBlock tb when tb.Text is { Length: > 0 }:
          return tb.Text;
        case ToolTip tt when tt.Content is string cs:
          return cs;
        case ToolTip tt2 when tt2.Content is TextBlock tbt && tbt.Text is { Length: > 0 }:
          return tbt.Text;
        default:
          return null;
      }
    }
  }
}
