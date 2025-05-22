using System.ComponentModel;
using System.Windows.Input;
using MainWindowProgram.Infrastructure;
using MainWindowProgram.Services;

namespace MainWindowProgram.ViewModels
{
  /// <summary>
  /// ViewModel для управления окном приложения.
  /// Содержит команды для изменения состояния окна, его закрытия, перетаскивания и адаптации интерфейса.
  /// </summary>
  public class WindowViewModel
  {
    /// <summary>
    /// Сервис управления состоянием окна.
    /// </summary>
    private readonly WindowService _service;

    /// <summary>
    /// Команда для сворачивания окна.
    /// </summary>
    public ICommand MinimizeCommand { get; }

    /// <summary>
    /// Команда для переключения состояния между обычным и развёрнутым.
    /// </summary>
    public ICommand MaximizeCommand { get; }

    /// <summary>
    /// Команда для перетаскивания окна по экрану.
    /// </summary>
    public ICommand DragMoveCommand { get; }

    /// <summary>
    /// Команда для завершения работы приложения.
    /// </summary>
    public ICommand CloseCommand { get; }

    /// <summary>
    /// Команда для адаптации размера шрифта главного меню в зависимости от размеров окна.
    /// </summary>
    public ICommand AdjustCommand { get; }

    /// <summary>
    /// Команда, вызываемая при закрытии окна.
    /// </summary>
    public ICommand ClosingCommand { get; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="WindowViewModel"/>.
    /// </summary>
    /// <param name="service">Сервис управления окном.</param>
    public WindowViewModel(WindowService service)
    {
      _service = service;

      MinimizeCommand = new AsyncRelayCommand(_service.MinimizeAsync);
      MaximizeCommand = new AsyncRelayCommand(_service.ToggleMaximizeAsync);
      DragMoveCommand = new AsyncRelayCommand(_service.DragMoveAsync);
      CloseCommand = new AsyncRelayCommand(_service.CloseApplicationAsync);
      AdjustCommand = new AsyncRelayCommand(_service.AdjustMainMenuFontAsync);

      // Обработка события Closing с передачей CancelEventArgs
      ClosingCommand = new AsyncRelayCommand(param =>
      {
        if (param is CancelEventArgs args)
          return _service.HandleWindowClosingAsync(args);

        return Task.CompletedTask;
      });
    }
  }
}
