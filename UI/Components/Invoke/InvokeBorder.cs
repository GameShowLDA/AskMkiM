using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UI.Components.Invoke
{
  public class InvokeBorder : Border
  {
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
