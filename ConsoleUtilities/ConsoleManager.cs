using System.Runtime.InteropServices;

namespace ConsoleUtilities
{
  public class ConsoleManager
  {
    private static ConsoleManager _instance;
    public static ConsoleManager Instance => _instance ??= new ConsoleManager();

    /// <summary>
    /// Событие изменения режима администратора вручную.
    /// </summary>
    public event EventHandler<bool> AdminModeChanged;

    private bool _isConsoleVisible = false;
    private readonly CommandHandler _commandHandler;

    private ConsoleManager()
    {
      _commandHandler = new CommandHandler();
      _commandHandler.AdminModeChanged += _commandHandler_AdminModeChanged;
      HideConsoleOnStart();
    }

    private void _commandHandler_AdminModeChanged(object? sender, bool e)
    {
      AdminModeChanged?.Invoke(sender, e);
    }

    private void HideConsoleOnStart()
    {
      IntPtr hWnd = GetConsoleWindow();
      if (hWnd != IntPtr.Zero)
      {
        ShowWindow(hWnd, SW_HIDE);
      }
    }

    public void ToggleConsole()
    {
      IntPtr consoleHWnd = GetConsoleWindow();

      if (consoleHWnd != IntPtr.Zero)
      {
        _isConsoleVisible = !_isConsoleVisible;

        if (_isConsoleVisible)
        {
          ShowWindow(consoleHWnd, SW_SHOW);
          ArrangeConsole(consoleHWnd);
        }
        else
        {
          ShowWindow(consoleHWnd, SW_HIDE);
        }
      }
    }

    private void ArrangeConsole(IntPtr consoleHWnd)
    {
      int screenWidth = GetSystemMetrics(SM_CXSCREEN);
      int screenHeight = GetSystemMetrics(SM_CYSCREEN);

      int consoleHeight = screenHeight / 2;
      int consoleWidth = screenWidth;
      int consoleX = 0;
      int consoleY = screenHeight - consoleHeight;

      MoveWindow(consoleHWnd, consoleX, consoleY, consoleWidth, consoleHeight, true);
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
  }
}
