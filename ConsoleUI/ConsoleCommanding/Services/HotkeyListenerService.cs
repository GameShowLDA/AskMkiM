using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Interop;

namespace ConsoleUI.ConsoleCommanding.Services
{
  public class HotkeyListenerService
  {
    private readonly Key _toggleKey;
    private readonly ModifierKeys _modifier;

    public event Action? HotkeyPressed;

    public HotkeyListenerService(Key toggleKey = Key.Oem3, ModifierKeys modifier = ModifierKeys.Control)
    {
      _toggleKey = toggleKey;
      _modifier = modifier;
      ComponentDispatcher.ThreadPreprocessMessage += OnKeyPressed;
    }

    private void OnKeyPressed(ref MSG msg, ref bool handled)
    {
      const int WM_KEYDOWN = 0x0100;
      if (msg.message == WM_KEYDOWN)
      {
        Key pressedKey = KeyInterop.KeyFromVirtualKey((int)msg.wParam);
        if (pressedKey == _toggleKey &&
            (Keyboard.Modifiers & _modifier) == _modifier)
        {
          HotkeyPressed?.Invoke();
          handled = true;
        }
      }
    }
  }
}
