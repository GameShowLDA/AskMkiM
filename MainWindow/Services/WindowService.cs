using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace MainWindowProgram.Services
{
  public class WindowService
  {
    /// <summary>
    /// Ссылка на главное окно приложения.
    /// </summary>
    private readonly MainWindow _mainWindow;

    /// <summary>
    /// Главное меню (элемент управления Menu).
    /// </summary>
    private readonly Menu _mainMenu;

    /// <summary>
    /// Панель с кнопками управления окном.
    /// </summary>
    private readonly StackPanel _buttonsPanel;

    /// <summary>
    /// Делегат, предоставляющий актуальное значение состояния блокировки приложения.
    /// </summary>
    private readonly Func<bool> _isLockedProvider;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="WindowService"/>.
    /// </summary>
    /// <param name="mainWindow">Главное окно.</param>
    /// <param name="mainMenu">Меню, отображаемое в окне.</param>
    /// <param name="buttonsPanel">Панель кнопок управления окном.</param>
    /// <param name="isLockedProvider">Функция, возвращающая признак блокировки интерфейса.</param>
    public WindowService(MainWindow mainWindow, Menu mainMenu, StackPanel buttonsPanel, Func<bool> isLockedProvider)
    {
      _mainWindow = mainWindow;
      _mainMenu = mainMenu;
      _buttonsPanel = buttonsPanel;
      _isLockedProvider = isLockedProvider;
    }

    /// <summary>
    /// Асинхронно изменяет размер шрифта в главном меню в зависимости от ширины окна.
    /// </summary>
    public async Task AdjustMainMenuFontAsync()
    {
      await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        var maxWidth = _mainWindow.ActualWidth - _mainMenu.ActualWidth - 50;
        var minWidth = 50 + _buttonsPanel.ActualWidth + _mainMenu.ActualWidth;

        double minFontSize = 11;
        double maxFontSize = 15;
        double minWindowWidth = 300;
        double maxWindowWidth = 800;

        double fontSize = minFontSize + ((maxFontSize - minFontSize) * ((maxWidth - minWindowWidth) / (maxWindowWidth - minWindowWidth)));

        fontSize = Math.Clamp(fontSize, minFontSize, maxFontSize);

        _mainMenu.FontSize = fontSize;
      });
    }

    /// <summary>
    /// Асинхронно завершает работу приложения.
    /// </summary>
    public Task CloseApplicationAsync()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        Application.Current.Shutdown();
      });

      return Task.CompletedTask;
    }

    /// <summary>
    /// Асинхронно запускает перетаскивание окна пользователем.
    /// </summary>
    public async Task DragMoveAsync()
    {
      await Application.Current.Dispatcher?.InvokeAsync(() =>
      {
        _mainWindow.DragMove();
      });
    }

    /// <summary>
    /// Асинхронно сворачивает окно в панель задач.
    /// </summary>
    public async Task MinimizeAsync()
    {
      await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        _mainWindow.WindowState = WindowState.Minimized;
      });
    }

    /// <summary>
    /// Асинхронно переключает состояние окна между нормальным и максимизированным.
    /// </summary>
    public async Task ToggleMaximizeAsync()
    {
      await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        if (_mainWindow.WindowState != WindowState.Maximized)
        {
          _mainWindow.WindowState = WindowState.Maximized;
        }
        else
        {
          _mainWindow.WindowState = WindowState.Normal;
        }
      });
    }

    /// <summary>
    /// Обрабатывает событие закрытия окна.
    /// </summary>
    /// <param name="e">Аргументы события Closing.</param>
    public async Task HandleWindowClosingAsync(CancelEventArgs e)
    {
      if (_isLockedProvider())
      {
        e.Cancel = true;
        MessageBox.Show(
            "Приложение заблокировано и не может быть закрыто.",
            "Внимание",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

      }
      else
      {
        Application.Current.Shutdown();
      }
    }
  }
}
