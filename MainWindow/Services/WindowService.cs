using System.ComponentModel;
using System.Windows;

namespace MainWindowProgram.Services
{
  public class WindowService
  {
    /// <summary>
    /// Ссылка на главное окно приложения.
    /// </summary>
    private readonly MainWindow _mainWindow;

    /// <summary>
    /// Делегат, предоставляющий актуальное значение состояния блокировки приложения.
    /// </summary>
    private readonly Func<bool> _isLockedProvider;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="WindowService"/>.
    /// </summary>
    /// <param name="mainWindow">Главное окно.</param>
    /// <param name="isLockedProvider">Функция, возвращающая признак блокировки интерфейса.</param>
    public WindowService(MainWindow mainWindow, Func<bool> isLockedProvider)
    {
      _mainWindow = mainWindow;
      _isLockedProvider = isLockedProvider;
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
    public void DragMoveAsync()
    {
      Application.Current.Dispatcher?.Invoke(() =>
      {
        try
        {
          _mainWindow.DragMove();
        }
        catch (Exception)
        {
          throw;
        }
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
