using System.Windows;
using System.Windows.Controls.Primitives;

namespace UI.Components.Invoke
{
  public class InvokeToggleButton : ToggleButton
  {
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
