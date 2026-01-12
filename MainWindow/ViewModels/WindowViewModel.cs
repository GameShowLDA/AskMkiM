using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MainWindowProgram.Services;
using System.ComponentModel;

namespace MainWindowProgram.ViewModels
{
  /// <summary>
  /// ViewModel для управления окном приложения.
  /// Содержит команды для изменения состояния окна, его закрытия, перетаскивания и адаптации интерфейса.
  /// </summary>
  public partial class WindowViewModel : ObservableObject
  {
    private readonly WindowService _service;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="WindowViewModel"/>.
    /// </summary>
    public WindowViewModel(WindowService service)
    {
      _service = service;
    }

    /// <summary>
    /// Команда для сворачивания окна.
    /// </summary>
    [RelayCommand]
    private async Task MinimizeAsync() => await _service.MinimizeAsync();

    /// <summary>
    /// Команда для переключения состояния между обычным и развёрнутым.
    /// </summary>
    [RelayCommand]
    private async Task MaximizeAsync() => await _service.ToggleMaximizeAsync();

    /// <summary>
    /// Команда для перетаскивания окна по экрану.
    /// </summary>
    [RelayCommand]
    private async Task DragMoveAsync() => await _service.DragMoveAsync();

    /// <summary>
    /// Команда для завершения работы приложения.
    /// </summary>
    [RelayCommand]
    private async Task CloseAsync() => await _service.CloseApplicationAsync();

    /// <summary>
    /// Команда для адаптации размера шрифта главного меню в зависимости от размеров окна.
    /// </summary>
    [RelayCommand]
    private async Task AdjustAsync() => await _service.AdjustMainMenuFontAsync();

    /// <summary>
    /// Команда, вызываемая при закрытии окна (с CancelEventArgs).
    /// </summary>
    [RelayCommand]
    private async Task ClosingAsync(CancelEventArgs args)
    {
      if (args != null)
        await _service.HandleWindowClosingAsync(args);
    }
  }
}
