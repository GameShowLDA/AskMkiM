using System.Runtime.InteropServices;
using ConsoleUtilities.Core;

namespace ConsoleUtilities.Services
{
  /// <summary>
  /// Управляет консольным окном и привязывает его к обработчику команд.
  /// </summary>
  public class ConsoleManager
  {
    private readonly ICommandHandler _handler;
    private bool _isConsoleVisible;
    private Task _inputLoop;


    public ConsoleManager(ICommandHandler handler)
    {
      _handler = handler;
      PositionConsoleWindow();
    }

    public void ToggleConsole()
    {
      var hwnd = GetConsoleWindow();
      ShowWindow(hwnd, _isConsoleVisible ? SW_HIDE : SW_SHOW);
      _isConsoleVisible = !_isConsoleVisible;

      if (_isConsoleVisible && (_inputLoop == null || _inputLoop.IsCompleted))
        _inputLoop = Task.Run(InputLoopAsync);
    }

    private async Task InputLoopAsync()
    {
      while (_isConsoleVisible)
      {
        var input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input))
          await _handler.HandleInputAsync(input);
      }
    }

    private void PositionConsoleWindow()
    {
      IntPtr hWnd = GetConsoleWindow();
      if (hWnd == IntPtr.Zero) return;

      int screenWidth = GetSystemMetrics(SM_CXSCREEN);
      int screenHeight = GetSystemMetrics(SM_CYSCREEN);

      int consoleHeight = screenHeight / 2;
      int consoleTop = screenHeight - consoleHeight;

      MoveWindow(hWnd, 0, consoleTop, screenWidth, consoleHeight, true);
    }

    [DllImport("kernel32.dll")] private static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
  }
}
