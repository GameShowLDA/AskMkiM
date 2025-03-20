using System.Windows;
using System.Windows.Controls.Primitives;

namespace UI.Components.Invoke
{
  /// <summary>
  /// Кастомный класс ToggleButton, который обеспечивает потокобезопасный доступ к его свойству видимости.
  /// Этот класс полезен для UI-компонентов, которые могут обновляться с не-UI потоков.
  /// </summary>
  public class InvokeToggleButton : ToggleButton
  {
    /// <summary>
    /// Получает или устанавливает видимость элемента.
    /// Свойство потокобезопасно и может быть доступно или изменено с не-UI потоков.
    /// </summary>
    public new Visibility Visibility
    {
      get
      {
        Visibility opacity = Visibility.Collapsed;
        Application.Current.Dispatcher.Invoke(() => opacity = base.Visibility);
        return opacity;
      }

      set
      {
        Application.Current.Dispatcher.Invoke(() => base.Visibility = value);
      }
    }
  }
}
