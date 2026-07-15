using System.Windows;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.UI.Features.ProtocolNew.Controls;

/// <summary>
/// Централизованно применяет состояния панели управления выполнением к кнопкам <c>ProtocolUI</c>.
/// </summary>
internal sealed class ProtocolButtonController
{
  /// <summary>
  /// Представление, содержащее реальные WPF-кнопки.
  /// </summary>
  private readonly IProtocolButtonView _view;

  /// <summary>
  /// Инициализирует контроллер указанным представлением кнопок.
  /// </summary>
  /// <param name="view">Минимальное представление видимости кнопок.</param>
  public ProtocolButtonController(IProtocolButtonView view)
  {
    _view = view;
  }

  /// <summary>
  /// Применяет указанное состояние панели управления.
  /// </summary>
  /// <param name="state">Целевое состояние панели.</param>
  /// <param name="stepMode">Нужно ли показывать пошаговые кнопки в поддерживающем их состоянии.</param>
  /// <param name="repeatVisible">Нужно ли показывать повтор во время паузы.</param>
  public void Apply(ProtocolButtonState state, bool stepMode = false, bool repeatVisible = false)
  {
    Application.Current.Dispatcher.Invoke(() =>
    {
      HideAllCore();

      switch (state)
      {
        case ProtocolButtonState.Hidden:
          break;

        case ProtocolButtonState.Ready:
          _view.StartVisibility = Visibility.Visible;
          break;

        case ProtocolButtonState.Running:
          _view.PauseVisibility = Visibility.Visible;
          _view.ExitVisibility = Visibility.Visible;
          SetStepButtons(stepMode);
          break;

        case ProtocolButtonState.Paused:
          _view.ContinueVisibility = Visibility.Visible;
          _view.ExitVisibility = Visibility.Visible;
          _view.RepeatVisibility = repeatVisible ? Visibility.Visible : Visibility.Collapsed;
          SetStepButtons(visible: true);
          break;

        case ProtocolButtonState.AdditionalActions:
          _view.LoopVisibility = Visibility.Visible;
          _view.RepeatVisibility = Visibility.Visible;
          _view.ExitVisibility = Visibility.Visible;
          break;

        case ProtocolButtonState.ExitOnly:
          _view.ExitVisibility = Visibility.Visible;
          SetStepButtons(stepMode);
          break;

        default:
          throw new ArgumentOutOfRangeException(nameof(state), state, "Неизвестное состояние панели кнопок.");
      }
    });
  }

  /// <summary>
  /// Обновляет только пошаговые кнопки с учётом текущего состояния основной панели.
  /// </summary>
  /// <param name="stepModeEnabled">Текущее состояние пошагового режима.</param>
  /// <param name="breakpointPauseActive">Находится ли выполнение на паузе точки останова.</param>
  public void UpdateStepMode(bool stepModeEnabled, bool breakpointPauseActive)
  {
    Application.Current.Dispatcher.Invoke(() =>
    {
      if (breakpointPauseActive)
      {
        Apply(ProtocolButtonState.Paused, stepModeEnabled, repeatVisible: false);
        return;
      }

      if (_view.ContinueVisibility == Visibility.Visible)
      {
        Apply(
          ProtocolButtonState.Paused,
          stepModeEnabled,
          _view.RepeatVisibility == Visibility.Visible);
        return;
      }

      if (_view.PauseVisibility == Visibility.Visible)
      {
        Apply(ProtocolButtonState.Running, stepModeEnabled);
        return;
      }

      SetStepButtons(visible: false);
    });
  }

  /// <summary>
  /// Скрывает только кнопки пошагового управления.
  /// </summary>
  public void HideStepButtons()
  {
    Application.Current.Dispatcher.Invoke(() => SetStepButtons(visible: false));
  }

  /// <summary>
  /// Скрывает кнопки, относящиеся к активному или приостановленному выполнению, не меняя остальные элементы.
  /// </summary>
  public void HideExecutionControls()
  {
    Application.Current.Dispatcher.Invoke(() =>
    {
      _view.PauseVisibility = Visibility.Collapsed;
      _view.ContinueVisibility = Visibility.Collapsed;
      _view.ExitVisibility = Visibility.Collapsed;
      SetStepButtons(visible: false);
    });
  }

  /// <summary>
  /// Скрывает все кнопки без повторного переключения Dispatcher.
  /// </summary>
  private void HideAllCore()
  {
    _view.StartVisibility = Visibility.Collapsed;
    _view.PauseVisibility = Visibility.Collapsed;
    _view.ContinueVisibility = Visibility.Collapsed;
    _view.ExitVisibility = Visibility.Collapsed;
    _view.RepeatVisibility = Visibility.Collapsed;
    _view.LoopVisibility = Visibility.Collapsed;
    SetStepButtons(visible: false);
  }

  /// <summary>
  /// Одновременно изменяет видимость обеих кнопок пошагового управления.
  /// </summary>
  /// <param name="visible">Признак требуемой видимости.</param>
  private void SetStepButtons(bool visible)
  {
    var visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    _view.StepOverVisibility = visibility;
    _view.StepIntoVisibility = visibility;
  }
}
