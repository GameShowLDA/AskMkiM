using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Threading;

namespace ConsoleUtilities.Services
{
  /// <summary>
  /// Слушает глобальный хоткей Ctrl + ~ и вызывает указанный обработчик.
  /// </summary>
  public class HotkeyListenerService : IDisposable
  {
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int HOTKEY_ID = 9000;
    private const uint MOD_CONTROL = 0x0002;
    private const uint VK_OEM3 = 0xC0; // Тильда `~`

    private readonly Action _onHotkeyPressed;
    private readonly IntPtr _windowHandle;

    public HotkeyListenerService(Window window, Action onHotkeyPressed)
    {
      _onHotkeyPressed = onHotkeyPressed ?? throw new ArgumentNullException(nameof(onHotkeyPressed));

      _windowHandle = new WindowInteropHelper(window).Handle;

      if (!RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL, VK_OEM3))
      {
        throw new InvalidOperationException("Не удалось зарегистрировать глобальный хоткей Ctrl + ~");
      }

      ComponentDispatcher.ThreadPreprocessMessage += OnThreadPreprocessMessage;
    }

    private void OnThreadPreprocessMessage(ref MSG msg, ref bool handled)
    {
      const int WM_HOTKEY = 0x0312;

      if (msg.message == WM_HOTKEY && msg.wParam.ToInt32() == HOTKEY_ID)
      {
        _onHotkeyPressed.Invoke();
        handled = true;
      }
    }

    public void Dispose()
    {
      UnregisterHotKey(_windowHandle, HOTKEY_ID);
      ComponentDispatcher.ThreadPreprocessMessage -= OnThreadPreprocessMessage;
    }
  }
}
