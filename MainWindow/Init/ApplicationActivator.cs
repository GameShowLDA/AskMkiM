using System.Runtime.InteropServices;
using System.Windows;

namespace MainWindowProgram.Init
{
  internal static class ApplicationActivator
  {
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    public static void ActivateMainWindow()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        Window? wnd = Application.Current.MainWindow;
        if (wnd == null) return;

        var handle = new System.Windows.Interop.WindowInteropHelper(wnd).Handle;

        // Если окно свернуто — восстановить
        ShowWindow(handle, SW_RESTORE);

        // Перевести фокус
        SetForegroundWindow(handle);
      });
    }
  }
}
