using System.Runtime.InteropServices;

namespace ConsoleUtilities
{
  /// <summary>
  /// Управляет видимостью и расположением консольного окна, а также взаимодействует с <see cref="CommandHandler"/>.
  /// </summary>
  public class ConsoleManager
  {
    private static ConsoleManager _instance;

    /// <summary>
    /// Единственный экземпляр <see cref="ConsoleManager"/>.
    /// </summary>
    public static ConsoleManager Instance => _instance ??= new ConsoleManager();

    /// <summary>
    /// Событие, вызываемое при изменении режима администратора.
    /// </summary>
    public event EventHandler<bool> AdminModeChanged;

    private bool _isConsoleVisible = false;
    private readonly CommandHandler _commandHandler;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ConsoleManager"/> и настраивает обработку команд.
    /// При инициализации консольное окно скрывается.
    /// </summary>
    private ConsoleManager()
    {
      _commandHandler = new CommandHandler();
      _commandHandler.AdminModeChanged += _commandHandler_AdminModeChanged;
      HideConsoleOnStart();
    }

    /// <summary>
    /// Обработчик события изменения режима администратора, получаемый от <see cref="CommandHandler"/>.
    /// Передает событие внешним подписчикам.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Логическое значение, указывающее, включен ли режим администратора.</param>
    private void _commandHandler_AdminModeChanged(object? sender, bool e)
    {
      AdminModeChanged?.Invoke(sender, e);
    }

    /// <summary>
    /// Скрывает консольное окно при старте приложения.
    /// </summary>
    private void HideConsoleOnStart()
    {
      IntPtr hWnd = GetConsoleWindow();
      if (hWnd != IntPtr.Zero)
      {
        ShowWindow(hWnd, SW_HIDE);
      }
    }

    /// <summary>
    /// Переключает видимость консольного окна.
    /// Если окно скрыто, оно становится видимым, и наоборот.
    /// При отображении окно располагается в нижней части экрана.
    /// </summary>
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

    /// <summary>
    /// Располагает консольное окно в нижней части экрана, растягивая его на всю ширину экрана и устанавливая высоту в половину от высоты экрана.
    /// </summary>
    /// <param name="consoleHWnd">Дескриптор консольного окна.</param>
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
