using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UI.Components.Invoke
{
  /// <summary>
  /// Класс, наследующий от <see cref="Border"/>, который позволяет безопасно 
  /// изменять свойство <see cref="BorderBrush"/> в многозадачной среде.
  /// </summary>
  public class InvokeBorder : Border
  {
    /// <summary>
    /// Переопределяет свойство <see cref="BorderBrush"/> с использованием диспетчера,
    /// чтобы обеспечить правильную работу в многозадачной среде.
    /// </summary>
    public new Brush BorderBrush
    {
      get
      {
        Brush brush = null;
        Application.Current.Dispatcher.Invoke(() => brush = base.BorderBrush);
        return brush;
      }

      set
      {
        Application.Current.Dispatcher.Invoke(() => base.BorderBrush = value);
      }
    }
  }
}
